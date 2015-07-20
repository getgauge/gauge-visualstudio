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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gauge.VisualStudio.Classification;
using Gauge.VisualStudio.Models;
using main;
using Microsoft.VisualStudio.PlatformUI.OleComponentSupport;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Gauge.VisualStudio.TestRunner
{
    [DefaultExecutorUri(TestExecutor.ExecutorUriString)]
    [FileExtension(".spec")]
    [FileExtension(".md")]
    public class TestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger,
            ITestCaseDiscoverySink discoverySink)
        {
            var testCases = GetSpecs(sources, discoverySink);
            foreach (var aTestCase in testCases)
            {
                discoverySink.SendTestCase(aTestCase);
            }
        }

        public static List<TestCase> GetSpecs(IEnumerable<string> sources, ITestCaseDiscoverySink discoverySink)
        {
            var testCases = new List<TestCase>();

            foreach (var protoSpec in SpecsHolder.Specs)
            {
                var scenarios = (from item in protoSpec.ItemsList
                    where item.HasScenario
                    select item
                    );

                var specificationName = protoSpec.SpecHeading;
                var scenarioIndex = 0;

                foreach (var scenario in scenarios)
                {
                    var testCase = new TestCase(string.Format("{0}.{1}", specificationName, scenario.Scenario.ScenarioHeading), TestExecutor.ExecutorUri, protoSpec.FileName)
                    {
                        CodeFilePath = protoSpec.FileName,
                        DisplayName = scenario.Scenario.ScenarioHeading,
                        // Ugly hack below - I don't know how else to pass the scenario index to GaugeRunner
                        // LocalExtensionData returns a null despite setting it here
                        LineNumber = scenarioIndex
                    };

                    if (discoverySink != null)
                    {
                        discoverySink.SendTestCase(testCase);
                    }
                    testCases.Add(testCase);

                    scenarioIndex++;
                }
            }
            return testCases;
        }
    }
}