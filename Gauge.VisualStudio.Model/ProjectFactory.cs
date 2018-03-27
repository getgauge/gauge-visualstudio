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

using Gauge.VisualStudio.Core.Extensions;
using System.Collections.Generic;

namespace Gauge.VisualStudio.Model
{
    public class ProjectFactory
    {
        private static Dictionary<string, IProject> projects = new Dictionary<string, IProject>();

        public static void Initialize(EnvDTE.Project vsProject)
        {
            if (projects.ContainsKey(vsProject.SlugifiedName()))
                return;
            projects.Add(vsProject.SlugifiedName(), new Project(() => vsProject));
        }

        public static IProject Get(EnvDTE.Project vsProject)
        {
            return Get(vsProject.SlugifiedName());
        }

        public static IProject Get(string slugifiedName)
        {
            IProject retval;
            projects.TryGetValue(slugifiedName, out retval);
            return retval;
        }

        public static void Delete(string slugifiedName)
        {
            if (projects.ContainsKey(slugifiedName))
                projects.Remove(slugifiedName);
        }
    }
}
