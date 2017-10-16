// Copyright [2014, 2015] [ThoughtWorks Inc.](www.thoughtworks.com)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.Serialization.Json;
using System.Text;
using EnvDTE;
using Gauge.CSharp.Core;
using Gauge.Messages;
using Gauge.VisualStudio.Core.Exceptions;
using Gauge.VisualStudio.Core.Extensions;
using Gauge.VisualStudio.Core.Helpers;
using Gauge.VisualStudio.Core.Loggers;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Process = System.Diagnostics.Process;
using Thread = System.Threading.Thread;

namespace Gauge.VisualStudio.Core
{
    public class GaugeService : IGaugeService
    {
        private static readonly Lazy<GaugeService> LazyInstance = new Lazy<GaugeService>(() => new GaugeService());

        private static readonly Dictionary<string, IGaugeApiConnection> ApiConnections =
            new Dictionary<string, IGaugeApiConnection>();

        private static readonly Dictionary<string, Process> ChildProcesses = new Dictionary<string, Process>();

        private static readonly Dictionary<string, int> ApiPorts = new Dictionary<string, int>();

        private static readonly List<Project> GaugeProjects = new List<Project>();
        public static readonly GaugeVersion MinGaugeVersion = new GaugeVersion("0.9.0");

        private GaugeService()
        {
        }

        public static IGaugeService Instance => LazyInstance.Value;

        public IEnumerable<IGaugeApiConnection> GetAllApiConnections()
        {
            return GaugeProjects.Select(GetApiConnectionFor);
        }

        public IGaugeApiConnection GetApiConnectionFor(Project project)
        {
            IGaugeApiConnection apiConnection;
            ApiConnections.TryGetValue(project.SlugifiedName(), out apiConnection);
            return apiConnection;
        }

        public void KillChildProcess(string slugifiedName)
        {
            if (ApiConnections.ContainsKey(slugifiedName))
                try
                {
                    ApiConnections.Remove(slugifiedName);
                    ApiConnections[slugifiedName].Dispose();
                }
                catch
                {
                }
            if (ChildProcesses.ContainsKey(slugifiedName))
                try
                {
                    ChildProcesses.Remove(slugifiedName);
                    ChildProcesses[slugifiedName].Kill();
                }
                catch
                {
                }
            if (ApiPorts.ContainsKey(slugifiedName))
                ApiPorts.Remove(slugifiedName);

            GaugeProjects.Remove(
                GaugeProjects.Find(project => string.CompareOrdinal(project.SlugifiedName(), slugifiedName) == 0));
        }

        public bool ContainsApiConnectionFor(string slugifiedName)
        {
            return ApiConnections.ContainsKey(slugifiedName);
        }

        public List<GaugeProjectProperties> GetPropertiesForAllGaugeProjects()
        {
            return GaugeProjects.Where(project => ApiPorts.ContainsKey(project.SlugifiedName()))
                .Select(project => new GaugeProjectProperties
                {
                    ApiPort = ApiPorts[project.SlugifiedName()],
                    BuildOutputPath = GetValidProjectOutputPath(project),
                    ProjectRoot = GetProjectRoot(project),
                    DaemonProcessId = ChildProcesses[project.SlugifiedName()].Id
                }).ToList();
        }

        public GaugeVersionInfo GetInstalledGaugeVersion(IGaugeProcess gaugeProcess = null)
        {
            if (gaugeProcess == null)
                gaugeProcess = GaugeProcess.ForVersion();

            gaugeProcess.Start();

            var error = gaugeProcess.StandardError.ReadToEnd();

            gaugeProcess.WaitForExit();

            if (gaugeProcess.ExitCode != 0)
                throw new GaugeVersionNotFoundException(error);
            var serializer = new DataContractJsonSerializer(typeof(GaugeVersionInfo));
            var versionText = SanitizeDeprecationMessage(gaugeProcess.StandardOutput.ReadToEnd());
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(versionText)))
            {
                return (GaugeVersionInfo) serializer.ReadObject(ms);
            }
        }

        public void DisplayGaugeNotStartedMessage(GaugeDisplayErrorLevel errorLevel, string dialogMessage,
            string errorMessageFormat, params object[] args)
        {
            var uiShell = (IVsUIShell) Package.GetGlobalService(typeof(IVsUIShell));
            var clsId = Guid.Empty;
            var result = 0;
            OutputPaneLogger.Error(string.Format(errorMessageFormat, args));
            OLEMSGICON msgicon;
            switch (errorLevel)
            {
                case GaugeDisplayErrorLevel.Info:
                    msgicon = OLEMSGICON.OLEMSGICON_INFO;
                    break;
                case GaugeDisplayErrorLevel.Warning:
                    msgicon = OLEMSGICON.OLEMSGICON_WARNING;
                    break;
                case GaugeDisplayErrorLevel.Error:
                    msgicon = OLEMSGICON.OLEMSGICON_CRITICAL;
                    break;
                default:
                    msgicon = OLEMSGICON.OLEMSGICON_NOICON;
                    break;
            }
            uiShell.ShowMessageBox(0, ref clsId,
                "Gauge - Error Occurred",
                dialogMessage,
                string.Empty,
                0,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                msgicon,
                0, out result
            );
        }

        public void RegisterGaugeProject(Project project, int minPortRange, int maxPortRange)
        {
            var apiConnection = StartGaugeAsDaemon(project, minPortRange, maxPortRange);
            if (apiConnection != null)
                ApiConnections.Add(project.SlugifiedName(), apiConnection);
            GaugeProjects.Add(project);
        }

        public void AssertCompatibility(IGaugeProcess gaugeProcess = null)
        {
            var installedGaugeVersion = GetInstalledGaugeVersion(gaugeProcess);

            if (new GaugeVersion(installedGaugeVersion.version).CompareTo(MinGaugeVersion) >= 0)
                return;

            var message = $"This plugin works with Gauge {MinGaugeVersion} or above." +
                          $" You have Gauge {installedGaugeVersion.version} installed." +
                          " Please update your Gauge installation.\n" +
                          " Run 'gauge version' from your command prompt for installation information.";

            throw new GaugeVersionIncompatibleException(message);
        }

        private static int GetOpenPort(int scanStart, int scanEnd)
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpEndPoints = properties.GetActiveTcpListeners();

            var portsInUse = tcpEndPoints.Select(p => p.Port).ToList();
            var unusedPort = 0;

            for (var port = scanStart; port < scanEnd; port++)
            {
                if (portsInUse.Contains(port)) continue;
                unusedPort = port;
                break;
            }
            return unusedPort;
        }

        private static string SanitizeDeprecationMessage(string message)
        {
            var lines = message.Split(Environment.NewLine.ToCharArray());
            if (lines[0].ToLower().StartsWith("[deprecated]"))
                lines = lines.Skip(1).ToArray();
            return string.Join(Environment.NewLine, lines);
        }

        private IGaugeApiConnection StartGaugeAsDaemon(Project gaugeProject, int minPortRange, int maxPortRange)
        {
            var slugifiedName = gaugeProject.SlugifiedName();
            if (ChildProcesses.ContainsKey(slugifiedName))
                if (ChildProcesses[slugifiedName].HasExited)
                    KillChildProcess(slugifiedName);
                else
                    return ApiConnections[slugifiedName];
            var projectOutputPath = GetValidProjectOutputPath(gaugeProject);

            var port = GetOpenPort(minPortRange, maxPortRange);
            OutputPaneLogger.Debug("Opening Gauge Daemon for Project : {0},  at port: {1}", gaugeProject.Name, port);

            ApiPorts.Add(slugifiedName, port);
            var environmentVariables = new Dictionary<string, string>
            {
                {"GAUGE_API_PORT", port.ToString(CultureInfo.InvariantCulture)},
                {"gauge_custom_build_path", projectOutputPath}
            };
            var gaugeProcess = GaugeProcess.ForDaemon(GetProjectRoot(gaugeProject), environmentVariables);

            if (!gaugeProcess.Start())
                throw new GaugeApiInitializationException(gaugeProcess.StandardOutput.ReadToEnd(),
                    gaugeProcess.StandardError.ReadToEnd());

            gaugeProcess.Exited += (s, e) => OutputPaneLogger.Debug($"PID {gaugeProcess.Id} has exited.");

            ChildProcesses.Add(slugifiedName, gaugeProcess.BaseProcess);
            OutputPaneLogger.Debug("Opening Gauge Daemon with PID: {0}", gaugeProcess.Id);
            var tcpClientWrapper = new TcpClientWrapper(port);
            WaitForColdStart(tcpClientWrapper);
            OutputPaneLogger.Debug("PID: {0} ready, waiting for messages..", gaugeProcess.Id);
            return new GaugeApiConnection(tcpClientWrapper);
        }

        private static string GetProjectRoot(Project gaugeProject)
        {
            return Path.GetDirectoryName(gaugeProject.FullName);
        }

        private static void WaitForColdStart(ITcpClientWrapper tcpClientWrapper)
        {
            while (!tcpClientWrapper.Connected)
                Thread.Sleep(100);

            var messageId = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            var specsRequest = new SpecsRequest();
            var apiMessage = new APIMessage
            {
                MessageId = messageId,
                MessageType = APIMessage.Types.APIMessageType.SpecsRequest,
                SpecsRequest = specsRequest
            };

            var gaugeApiConnection = new GaugeApiConnection(tcpClientWrapper);
            var i = 0;
            while (i < 10)
            {
                try
                {
                    var message = gaugeApiConnection.WriteAndReadApiMessage(apiMessage);
                    if (message.SpecsResponse != null && message.SpecsResponse.Details.Count > 0)
                        break;
                }
                catch
                {
                    //do nothing, count as a retry attempt
                }
                Thread.Sleep(500);
                i++;
            }
        }

        private static string GetValidProjectOutputPath(Project gaugeProject)
        {
            var projectOutputPath = gaugeProject.GetProjectOutputPath();

            if (string.IsNullOrEmpty(projectOutputPath))
            {
                OutputPaneLogger.Error(
                    "Unable to retrieve Project Output path for Project : {0}. Not starting Gauge Daemon",
                    gaugeProject.Name);
                throw new GaugeApiInitializationException("",
                    $"Unable to retrieve Project Output path for Project : {gaugeProject.Name}");
            }

            if (Directory.Exists(projectOutputPath))
                return projectOutputPath;

            OutputPaneLogger.Error("Project Output path '{0}' does not exists. Not starting Gauge Daemon",
                projectOutputPath);
            throw new GaugeApiInitializationException("", $"Project Output path '{projectOutputPath}' does not exists");
        }
    }
}