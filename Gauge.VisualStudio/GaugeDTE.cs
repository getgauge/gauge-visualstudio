using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using EnvDTE;
using Gauge.CSharp.Lib;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Process = System.Diagnostics.Process;

namespace Gauge.VisualStudio
{

    [Export(typeof(IClassifierProvider))]
    [ContentType("spec")]
    internal class GaugeDTEProvider : IClassifierProvider, IDisposable
    {
        public static DTE DTE { get; private set; }

        [Import]
        internal SVsServiceProvider ServiceProvider = null;

        private static readonly Dictionary<string, GaugeApiConnection> ApiConnections = new Dictionary<string, GaugeApiConnection>();

        private static readonly HashSet<Process> ChildProcesses = new HashSet<Process>();

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            DTE = (DTE)ServiceProvider.GetService(typeof(DTE));
            foreach (Project project in DTE.Solution.Projects)
            {
                var vsProject = project.Object as VSLangProj.VSProject;
                if (vsProject == null || vsProject.References.Find("Gauge.CSharp.Lib") == null) continue;
                var gaugeProject = vsProject.Project;
                if (ApiConnections.ContainsKey(SlugifyName(vsProject.Project)))
                    return null;

                var openPort = GetOpenPort();

                StartGaugeAsDaemon(gaugeProject, openPort);

                var apiConnection = new GaugeApiConnection(new TcpClientWrapper(openPort));
                ApiConnections.Add(SlugifyName(gaugeProject), apiConnection);
                Debug.WriteLine("project: {0}, port: {1}", SlugifyName(gaugeProject), openPort);
            }
            return null;
        }

        public static GaugeApiConnection GetApiConnectionForActiveDocument()
        {
            return ApiConnections[SlugifyName(DTE.ActiveDocument.ProjectItem.ContainingProject)];
        }

        private static string SlugifyName(Project gaugeProject)
        {
            return gaugeProject.Name.Replace('.', '_');
        }

        private static void StartGaugeAsDaemon(Project gaugeProject, int openPort)
        {
            var gaugeStartInfo = new ProcessStartInfo
            {
                WorkingDirectory = Path.GetDirectoryName(gaugeProject.FullName),
                UseShellExecute = false,
                FileName = "gauge.exe",
                CreateNoWindow = true,
                Arguments = string.Format("--daemonize")
            };

            gaugeStartInfo.EnvironmentVariables["GAUGE_API_PORT"] = openPort.ToString(CultureInfo.InvariantCulture);

            var gaugeProcess = new Process
            {
                StartInfo = gaugeStartInfo
            };
            gaugeProcess.Start();
            gaugeProcess.WaitForExit(5000); //timeout = 5 seconds to launch gauge

            ChildProcesses.Add(gaugeProcess);
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

        public void Dispose()
        {
            foreach (var childProcess in ChildProcesses)
            {
                childProcess.Kill();
            }
        }
    }
}
