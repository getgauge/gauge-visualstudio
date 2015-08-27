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
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using VSLangProj;

namespace Gauge.VisualStudio
{

    [Export(typeof(IClassifierProvider))]
    [Order(Before = "default")]
    [ContentType(GaugeContentTypeDefinitions.GaugeContentType)]
    internal class GaugeDTEProvider : IClassifierProvider
    {
        [Import(typeof(SVsServiceProvider))]
        internal IServiceProvider ServiceProvider = null;

        public static readonly Dictionary<string, Dictionary<string, TextPoint>> ConceptDictionary = new Dictionary<string, Dictionary<string, TextPoint>>();

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            return null;
        }

        public static IEnumerable<VSProject> GetGaugeProjects(IServiceProvider service)
        {
            var projects = GaugePackage.DTE.Solution.Projects;
            for (var i = 1; i <= projects.Count; i++)
            {
                var vsProject = projects.Item(i).Object as VSProject;
                if (vsProject == null || vsProject.References.Find("Gauge.CSharp.Lib") == null) continue;
                yield return vsProject;
            }
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
    }
}
