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
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Gauge.VisualStudio.Extensions
{
    internal static class ProjectExtensions
    {
        public static string SlugifiedName(this Project project)
        {
            return project == null ? "" : project.Name.Replace('.', '_');
        }

        public static bool IsGaugeProject(this Project project)
        {
            try
            {
                var directoryName = Path.GetDirectoryName(project.FileName);
                var manifestExists = File.Exists(Path.Combine(directoryName, "manifest.json"));
                var specsDirExists = Directory.Exists(Path.Combine(directoryName, "specs"));

                return specsDirExists && manifestExists;
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
    }
}
