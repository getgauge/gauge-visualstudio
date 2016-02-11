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

using System.IO;
using System.Text.RegularExpressions;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Gauge.VisualStudio.Core.Extensions
{
    public static class ProjectExtensions
    {
        private static readonly Regex ManifestExists = new Regex("\"Language\"\\s*:\\s*\"csharp\"", RegexOptions.Compiled | RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string SlugifiedName(this Project project)
        {
            return project == null ? "" : project.Name.Replace('.', '_');
        }

        public static bool IsGaugeProject(this Project project)
        {
            try
            {
                var directoryName = Path.GetDirectoryName(project.FileName);
                var manifestPath = Path.Combine(directoryName, "manifest.json");
                if (!File.Exists(manifestPath))
                {
                    return false;
                }

                var manifest = File.ReadAllText(manifestPath);
                return ManifestExists.IsMatch(manifest);
            }
            catch
            {
                return false;
            }
        }

        public static Project ToProject(this IVsHierarchy hierarchy)
        {
            object objProj;
            hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out objProj);

            return objProj as Project;
        }

        public static string GetProjectOutputPath(this Project project)
        {
            var configurationManager = project.ConfigurationManager;
            var activeConfiguration = configurationManager.ActiveConfiguration;
            var outputPath = activeConfiguration.Properties.Item("OutputPath").Value.ToString();
            return Path.IsPathRooted(outputPath)
                ? outputPath
                : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project.FullName), outputPath));
        }
    }
}
