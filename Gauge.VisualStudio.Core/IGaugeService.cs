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

using System.Collections.Generic;
using EnvDTE;
using Gauge.CSharp.Core;
using Gauge.VisualStudio.Core.Helpers;

namespace Gauge.VisualStudio.Core
{
    public interface IGaugeService
    {
        IEnumerable<IGaugeApiConnection> GetAllApiConnections();
        IGaugeApiConnection GetApiConnectionFor(Project project);
        void KillChildProcess(string slugifiedName);
        bool ContainsApiConnectionFor(string slugifiedName);
        List<GaugeProjectProperties> GetPropertiesForAllGaugeProjects();
        GaugeVersionInfo GetInstalledGaugeVersion(IGaugeProcess gaugeProcess = null);
        void AssertCompatibility(IGaugeProcess gaugeProcess = null);

        void DisplayGaugeNotStartedMessage(GaugeDisplayErrorLevel errorLevel, string dialogMessage, string errorMessageFormat, params object[] args);
        void RegisterGaugeProject(Project project, int minPortRange, int maxPortRange);
    }
}