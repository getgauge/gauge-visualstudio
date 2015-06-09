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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using EnvDTE;
using Gauge.CSharp.Lib;
using Gauge.VisualStudio.Extensions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using VSLangProj;
using Process = System.Diagnostics.Process;

namespace Gauge.VisualStudio
{

    [Export(typeof(IClassifierProvider))]
    [Order(Before = "default")]
    [ContentType(GaugeContentTypeDefinitions.GaugeContentType)]
    internal class GaugeDTEProvider : IClassifierProvider, IDisposable
    {
        public static DTE DTE { get; private set; }

        [Import(typeof(SVsServiceProvider))]
        internal IServiceProvider ServiceProvider = null;

        private static readonly Dictionary<string, GaugeApiConnection> ApiConnections = new Dictionary<string, GaugeApiConnection>();

        public static readonly Dictionary<string, Dictionary<string, TextPoint>> ConceptDictionary = new Dictionary<string, Dictionary<string, TextPoint>>();

        private static readonly HashSet<Process> ChildProcesses = new HashSet<Process>();

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            var projects = GetGaugeProjects(ServiceProvider);
            foreach (var vsProject in projects)
            {
                var gaugeProject = vsProject.Project;
                if (ApiConnections.ContainsKey(vsProject.Project.SlugifiedName()))
                    return null;

                var openPort = GetOpenPort();
                StartGaugeAsDaemon(gaugeProject, openPort);

                var apiConnection = new GaugeApiConnection(new TcpClientWrapper(openPort));
                ApiConnections.Add(gaugeProject.SlugifiedName(), apiConnection);
            }
            return null;
        }

        public static IEnumerable<VSProject> GetGaugeProjects(IServiceProvider service)
        {
            DTE = GetDTE(service);
            var projects = DTE.Solution.Projects;
            for (var i = 1; i <= projects.Count; i++)
            {
                var vsProject = projects.Item(i).Object as VSProject;
                if (vsProject == null || vsProject.References.Find("Gauge.CSharp.Lib") == null) continue;
                yield return vsProject;
            }
        }

        private static DTE GetDTE(IServiceProvider service)
        {
            return (DTE)service.GetService(typeof(DTE));
        }

        public static IEnumerable<string> GetAllSpecs(IServiceProvider service)
        {
            var specs = new List<string>();
            foreach (var gaugeProject in GetGaugeProjects(service))
                specs.AddRange(ScanProject(gaugeProject.Project.ProjectItems));

            return specs.Where(s => s.EndsWith(".spec") | s.EndsWith(".md"));
        }

        private static IEnumerable<string> ScanProject(IEnumerable projectItems)
        {
            foreach (ProjectItem item in projectItems)
            {
                yield return item.FileNames[0];

                foreach (var childItem in ScanProject(item.ProjectItems))
                    yield return childItem;
            }
        }

        public static GaugeApiConnection GetApiConnectionForActiveDocument()
        {
            return ApiConnections[DTE.ActiveDocument.ProjectItem.ContainingProject.SlugifiedName()];
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
            if(gaugeProcess.Start())
                ChildProcesses.Add(gaugeProcess);

            //TODO: Check process exitcode and handle failure
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

        public static GaugeApiConnection GetApiConnectionFor(Project project)
        {
            GaugeApiConnection apiConnection;
            ApiConnections.TryGetValue(project.SlugifiedName(), out apiConnection);
            return apiConnection;
        }
    }
}
