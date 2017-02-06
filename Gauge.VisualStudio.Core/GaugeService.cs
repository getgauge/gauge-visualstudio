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
using EnvDTE;
using Gauge.CSharp.Core;
using Gauge.Messages;
using Gauge.VisualStudio.Core.Exceptions;
using Gauge.VisualStudio.Core.Extensions;
using Gauge.VisualStudio.Core.Helpers;
using Gauge.VisualStudio.Core.Loggers;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Process = System.Diagnostics.Process;
using Thread = System.Threading.Thread;

namespace Gauge.VisualStudio.Core
{
    public class GaugeService : IGaugeService
    {
        private static readonly Lazy<GaugeService> LazyInstance = new Lazy<GaugeService>(() => new GaugeService());

        private GaugeService()
        {
        }

        public static IGaugeService Instance
        {
            get { return LazyInstance.Value; }
        }

        private static readonly Dictionary<string, IGaugeApiConnection> ApiConnections =
            new Dictionary<string, IGaugeApiConnection>();

        private static readonly Dictionary<string, Process> ChildProcesses = new Dictionary<string, Process>();

        private static readonly Dictionary<string, PortInfo> PortsInfo = new Dictionary<string, PortInfo>();

        private static readonly List<Project> GaugeProjects = new List<Project>();
        public static readonly GaugeVersion MinGaugeVersion = new GaugeVersion("0.8.0");

        public void RegisterGaugeProject(Project project)
        {
            GaugeProjects.Add(project);
        }

        public IEnumerable<IGaugeApiConnection> GetAllApiConnections()
        {
            return GaugeProjects.Select(project => GetApiConnectionFor(project));
        }

        public IGaugeApiConnection GetApiConnectionFor(Project project)
        {
            IGaugeApiConnection apiConnection;
            if (ApiConnections.TryGetValue(project.SlugifiedName(), out apiConnection))
                return apiConnection;
            apiConnection = StartGaugeAsDaemon(project);
            if (apiConnection != null)
            {
                ApiConnections.Add(project.SlugifiedName(), apiConnection);
            }
            return apiConnection;
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

        public void KillChildProcess(string slugifiedName)
        {
            if (ApiConnections.ContainsKey(slugifiedName))
            {
                try
                {
                    ApiConnections[slugifiedName].Dispose();
                    ApiConnections.Remove(slugifiedName);
                }
                catch
                {
                }
            }
            if (ChildProcesses.ContainsKey(slugifiedName))
            {
                try
                {
                    ChildProcesses[slugifiedName].Kill();
                    ChildProcesses.Remove(slugifiedName);
                }
                catch
                {
                }
            }
            if (PortsInfo.ContainsKey(slugifiedName))
            {
                PortsInfo.Remove(slugifiedName);
            }

            GaugeProjects.Remove(
                GaugeProjects.Find(project => string.CompareOrdinal(project.SlugifiedName(), slugifiedName) == 0));
        }

        public bool ContainsApiConnectionFor(string slugifiedName)
        {
            return ApiConnections.ContainsKey(slugifiedName);
        }

        public List<GaugeProjectProperties> GetPropertiesForAllGaugeProjects()
        {
            return GaugeProjects.Where(project => PortsInfo.ContainsKey(project.SlugifiedName()))
                .Select( project => new GaugeProjectProperties
                    {
                        ApiPort = PortsInfo[project.SlugifiedName()].ApiPort,
                        ApiV2Port = PortsInfo[project.SlugifiedName()].ApiV2Port,
                        BuildOutputPath = GetValidProjectOutputPath(project),
                        ProjectRoot = GetProjectRoot(project),
                        DaemonProcessId = ChildProcesses[project.SlugifiedName()].Id
                    }).ToList();
        }

        public GaugeVersionInfo GetInstalledGaugeVersion(IGaugeProcess gaugeProcess = null)
        {
            if (gaugeProcess == null)
            {
                gaugeProcess = GaugeProcess.ForVersion();
            }

            gaugeProcess.Start();

            var error = gaugeProcess.StandardError.ReadToEnd();

            gaugeProcess.WaitForExit();

            if (gaugeProcess.ExitCode != 0)
            {
                throw new GaugeVersionNotFoundException(error);
            }
            var serializer = new DataContractJsonSerializer(typeof (GaugeVersionInfo));
            return (GaugeVersionInfo) serializer.ReadObject(gaugeProcess.StandardOutput.BaseStream);
        }

        private IGaugeApiConnection StartGaugeAsDaemon(Project gaugeProject)
        {
            var slugifiedName = gaugeProject.SlugifiedName();
            if (ChildProcesses.ContainsKey(slugifiedName))
            {
                if (ChildProcesses[slugifiedName].HasExited)
                {
                    KillChildProcess(slugifiedName);
                }
                else
                {
                    return ApiConnections[slugifiedName];
                }
            }
            var waitDialog = (IVsThreadedWaitDialog)Package.GetGlobalService(typeof(SVsThreadedWaitDialog));
            try
            {
                ErrorHandler.ThrowOnFailure(waitDialog.StartWaitDialog("Initializing Gauge Project",
                    string.Format("Initializing Gauge daemon for Project: {0}", gaugeProject.Name),
                    null,
                    0,
                    null,
                    null));
                var projectOutputPath = GetValidProjectOutputPath(gaugeProject);

                var portInfo = new PortInfo(GetOpenPort(1000, 2000), GetOpenPort(2000, 3000));
                OutputPaneLogger.Debug("Opening Gauge Daemon for Project : {0},  at ports: {1}, {2}", gaugeProject.Name,
                    portInfo.ApiPort, portInfo.ApiV2Port);

                PortsInfo.Add(slugifiedName, portInfo);
                var environmentVariables = new Dictionary<string, string>
                {
                    {"GAUGE_API_PORT", portInfo.ApiPort.ToString(CultureInfo.InvariantCulture)},
                    {"GAUGE_API_V2_PORT", portInfo.ApiV2Port.ToString(CultureInfo.InvariantCulture)},
                    {"gauge_custom_build_path", projectOutputPath}
                };
                var gaugeProcess = GaugeProcess.ForDaemon(GetProjectRoot(gaugeProject), environmentVariables);

                try
                {
                    if (gaugeProcess.Start())
                    {
                        ChildProcesses.Add(slugifiedName, gaugeProcess.BaseProcess);
                    }
                    OutputPaneLogger.Debug("Opening Gauge Daemon with PID: {0}", gaugeProcess.Id);
                    var tcpClientWrapper = new TcpClientWrapper(portInfo.ApiPort);
                    WaitForColdStart(tcpClientWrapper);
                    OutputPaneLogger.Debug("PID: {0} ready, waiting for messages..", gaugeProcess.Id);
                    return new GaugeApiConnection(tcpClientWrapper);
                }
                catch (Exception ex)
                {
                    var errorMessage = string.Format("Failed to start Gauge Daemon: {0}", ex);
                    DisplayGaugeNotStartedMessage("Unable to launch Gauge Daemon. Check Output Window for details", errorMessage, GaugeDisplayErrorLevel.Error);
                    return null;
                }
            }
            finally
            {
                var cancelled = 0;
                if (waitDialog != null)
                {
                    waitDialog.EndWaitDialog(ref cancelled);
                }
            }
        }

        private static string GetProjectRoot(Project gaugeProject)
        {
            return Path.GetDirectoryName(gaugeProject.FullName);
        }

        public void DisplayGaugeNotStartedMessage(string dialogMessage, string errorMessageFormat, GaugeDisplayErrorLevel errorLevel, params object[] args)
        {
            var uiShell = (IVsUIShell) Package.GetGlobalService(typeof (IVsUIShell));
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

        private static void WaitForColdStart(ITcpClientWrapper tcpClientWrapper)
        {
            while (!tcpClientWrapper.Connected)
            {
                Thread.Sleep(100);
            }

            var messageId = DateTime.Now.Ticks/TimeSpan.TicksPerMillisecond;
            var specsRequest = new SpecsRequest();
            var apiMessage = new APIMessage()
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
                    {
                        break;
                    }
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
                throw new GaugeApiInitializationException();
            }

            if (!Directory.Exists(projectOutputPath))
            {
                OutputPaneLogger.Error("Project Output path '{0}' does not exists. Not starting Gauge Daemon",
                    projectOutputPath);
                throw new GaugeApiInitializationException();
            }

            if (!Directory.EnumerateFiles(projectOutputPath, "*.dll").Any())
            {
                OutputPaneLogger.Info(
                    "Project Output path '{0}' does not contain any binaries. Building the project", projectOutputPath);
                if (!gaugeProject.DTE.Solution.SolutionBuild.BuildState.Equals(vsBuildState.vsBuildStateInProgress))
                {
                    gaugeProject.DTE.Solution.SolutionBuild.Build(true);
                }
            }

            return projectOutputPath;
        }

        public void AssertCompatibility(IGaugeProcess gaugeProcess = null)
        {
            var installedGaugeVersion = GetInstalledGaugeVersion(gaugeProcess);

            if (new GaugeVersion(installedGaugeVersion.version).CompareTo(MinGaugeVersion) >= 0)
            {
                return;
            }

            var message = string.Format("This plugin works with Gauge {0} or above. You have Gauge {1} installed. Please update your Gauge installation.\n" +
                                        " Run gauge -v from your command prompt for installation information.", MinGaugeVersion, installedGaugeVersion.version);

            throw new GaugeVersionIncompatibleException(message);
        }
    }
}
