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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using EnvDTE;
using Gauge.CSharp.Core;
using Gauge.Messages;
using Gauge.VisualStudio.Core.Exceptions;
using Gauge.VisualStudio.Core.Extensions;
using Gauge.VisualStudio.Core.Loggers;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Process = System.Diagnostics.Process;
using Thread = System.Threading.Thread;

namespace Gauge.VisualStudio.Core.Helpers
{
    public class GaugeDaemonHelper
    {
        private static readonly Dictionary<string, GaugeApiConnection> ApiConnections = new Dictionary<string, GaugeApiConnection>();

        private static readonly Dictionary<string, Process> ChildProcesses = new Dictionary<string, Process>();

        private static readonly Dictionary<string, int> ApiPorts = new Dictionary<string, int>();

        private static readonly List<Project> GaugeProjects = new List<Project>();

        public static void RegisterGaugeProject(Project project)
        {
            GaugeProjects.Add(project);
        }

        public static IEnumerable<GaugeApiConnection> GetAllApiConnections()
        {
            return GaugeProjects.Select(GetApiConnectionFor);
        }

        public static GaugeApiConnection GetApiConnectionFor(Project project)
        {
            GaugeApiConnection apiConnection;
            if (ApiConnections.TryGetValue(project.SlugifiedName(), out apiConnection))
                return apiConnection;
            apiConnection = StartGaugeAsDaemon(project);
            if (apiConnection != null)
            {
                ApiConnections.Add(project.SlugifiedName(), apiConnection);
            }
            return apiConnection;
        }

        private static int GetOpenPort()
        {
            const int scanStart = 1000;
            const int scanEnd = 2000;
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

        public static void KillChildProcess(string slugifiedName)
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
            if (ApiPorts.ContainsKey(slugifiedName))
            {
                ApiPorts.Remove(slugifiedName);
            }

            GaugeProjects.Remove(
                GaugeProjects.Find(project => string.CompareOrdinal(project.SlugifiedName(), slugifiedName) == 0));
        }

        public static bool ContainsApiConnectionFor(string slugifiedName)
        {
            return ApiConnections.ContainsKey(slugifiedName);
        }

        public static List<GaugeProjectProperties> GetPropertiesForAllGaugeProjects()
        {
            return GaugeProjects.Select( project =>
                new GaugeProjectProperties
                {
                    ApiPort = ApiPorts[project.SlugifiedName()],
                    BuildOutputPath = GetValidProjectOutputPath(project),
                    ProjectRoot = GetProjectRoot(project)
                }).ToList();
        }

        private static GaugeApiConnection StartGaugeAsDaemon(Project gaugeProject)
        {
            var waitDialog = (IVsThreadedWaitDialog)Package.GetGlobalService(typeof(SVsThreadedWaitDialog));
            try
            {
                waitDialog.StartWaitDialog("Initializing Gauge Project",
                    string.Format("Initializing Gauge daemon for Project: {0}", gaugeProject.Name),
                    null,
                    0,
                    null,
                    null);
                var projectOutputPath = GetValidProjectOutputPath(gaugeProject);

                var openPort = GetOpenPort();
                OutputPaneLogger.Debug("Opening Gauge Daemon for Project : {0},  at port: {1}", gaugeProject.Name,
                    openPort);
                var slugifiedName = gaugeProject.SlugifiedName();

                ApiPorts.Add(slugifiedName, openPort);
                var gaugeStartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = GetProjectRoot(gaugeProject),
                    UseShellExecute = false,
                    FileName = "gauge.exe",
                    CreateNoWindow = true,
                    Arguments = "--daemonize",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                gaugeStartInfo.EnvironmentVariables["GAUGE_API_PORT"] = openPort.ToString(CultureInfo.InvariantCulture);
                gaugeStartInfo.EnvironmentVariables["gauge_custom_build_path"] = projectOutputPath;

                var gaugeProcess = new Process { StartInfo = gaugeStartInfo };

                try
                {
                    if (gaugeProcess.Start())
                    {
                        ChildProcesses.Add(slugifiedName, gaugeProcess);
                    }
                    OutputPaneLogger.Debug("Opening Gauge Daemon with PID: {0}", gaugeProcess.Id);
                    var tcpClientWrapper = new TcpClientWrapper(openPort);
                    WaitForColdStart(tcpClientWrapper);
                    OutputPaneLogger.Debug("PID: {0} ready, waiting for messages..", gaugeProcess.Id);
                    return new GaugeApiConnection(tcpClientWrapper);
                }
                catch
                {
                    DisplayGaugeNotFoundMessage();
                    return null;
                }
            }
            finally
            {
                var cancelled = 0;
                waitDialog.EndWaitDialog(ref cancelled);
            }
        }

        private static string GetProjectRoot(Project gaugeProject)
        {
            return Path.GetDirectoryName(gaugeProject.FullName);
        }

        private static void DisplayGaugeNotFoundMessage()
        {
            const string message = "Unable to launch Gauge Daemon. Ensure that\n" +
                                   "1. Gauge is installed.\n" +
                                   "2. Gauge.exe is available in PATH\n\n" +
                                   "If issue persists, please report to authors";
            var uiShell = (IVsUIShell) Package.GetGlobalService(typeof (IVsUIShell));
            var clsId = Guid.Empty;
            var result = 0;
            uiShell.ShowMessageBox(0, ref clsId,
                "Gauge - Critical Error Occurred",
                message,
                string.Empty,
                0,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                OLEMSGICON.OLEMSGICON_CRITICAL,
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
            var specsRequest = GetAllSpecsRequest.DefaultInstance;
            var apiMessage = APIMessage.CreateBuilder()
                .SetMessageId(messageId)
                .SetMessageType(APIMessage.Types.APIMessageType.GetAllSpecsRequest)
                .SetAllSpecsRequest(specsRequest)
                .Build();

            var gaugeApiConnection = new GaugeApiConnection(tcpClientWrapper);
            var i = 0;
            while (i < 10)
            {
                var message = gaugeApiConnection.WriteAndReadApiMessage(apiMessage);
                if (message.HasAllSpecsResponse && message.AllSpecsResponse.SpecsCount > 0)
                {
                    break;
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
                OutputPaneLogger.Error(
                    "Project Output path '{0}' does not contain any binaries. Ensure that project is built. Not starting Gauge Daemon",
                    projectOutputPath);
                throw new GaugeApiInitializationException();
            }

            return projectOutputPath;
        }
    }
}