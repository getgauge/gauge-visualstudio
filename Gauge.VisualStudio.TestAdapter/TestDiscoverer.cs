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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gauge.Messages;
using Gauge.VisualStudio.Core.Helpers;
using Gauge.VisualStudio.Core.Loggers;
using Gauge.VisualStudio.Model;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Gauge.VisualStudio.TestAdapter
{
    [FileExtension(".md")]
    [FileExtension(".spec")]
    [DefaultExecutorUri(TestExecutor.ExecutorUriString)]
    public class TestDiscoverer : ITestDiscoverer
    {
        public static readonly TestProperty GaugeCustomBuildPath = TestProperty.Register("TestCase.GaugeCustomBuildPath",
            "Custom build path for Gauge project binaries.", typeof (string), typeof (TestCase));
        public static readonly TestProperty GaugeProjectRoot = TestProperty.Register("TestCase.GaugeProjectRoot",
            "GAUGE_PROJECT_ROOT value set in Gauge.", typeof (string), typeof (TestCase));
        public static readonly TestProperty ScenarioIndex = TestProperty.Register("TestCase.ScenarioIndex",
            "Index of Scenario in a given spec. Starts with 0.", typeof (int), typeof (TestCase));

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger,
            ITestCaseDiscoverySink discoverySink)
        {
            var settingsProvider = discoveryContext.RunSettings.GetSettings(GaugeTestRunSettings.SettingsName) 
                as GaugeTestRunSettingsService;
            GetSpecs(settingsProvider.Settings, discoverySink, sources, logger);
        }

        public static List<TestCase> GetSpecs(GaugeTestRunSettings testRunSettings, ITestCaseDiscoverySink discoverySink, IEnumerable<string> sources, IMessageLogger logger)
        {
            var gaugeProjectProperties = testRunSettings.ProjectsProperties;
            OutputPaneLogger.Debug("Discover Scenarios started. Using API ports: {0}", gaugeProjectProperties);

            var testCases = new ConcurrentBag<TestCase>();

            Parallel.ForEach(gaugeProjectProperties, properties =>
            {
                var protoSpecs = Specification.GetAllSpecs(properties.ApiPort);

                Parallel.ForEach(protoSpecs, spec =>
                {
                    OutputPaneLogger.Debug("Source: {0}", spec);

                    if (sources.All(s => string.CompareOrdinal(s, spec.FileName) != 0))
                        return;

                    var scenarioIndex = 0;
                    var scenarios = spec.ItemsList.Where(item => item.HasScenario).Select(item => item.Scenario);

                    foreach (var scenario in scenarios)
                    {
                        var testCase = CreateTestCase(logger, spec, scenario, properties, scenarioIndex);
                        testCases.Add(testCase);

                        if (discoverySink != null)
                        {
                            discoverySink.SendTestCase(testCase);
                        }

                        scenarioIndex++;
                    }
                });
            });
            return testCases.ToList();
        }

        private static TestCase CreateTestCase(IMessageLogger logger, ProtoSpec spec, ProtoScenario scenario,
            GaugeProjectProperties properties, int scenarioIndex)
        {
            var testCase = new TestCase(string.Format("[{0}].[{1}]", spec.SpecHeading, scenario.ScenarioHeading),
                    TestExecutor.ExecutorUri, spec.FileName)
                {
                    CodeFilePath = spec.FileName,
                    DisplayName = scenario.ScenarioHeading
                };

            testCase.SetPropertyValue(GaugeCustomBuildPath, properties.BuildOutputPath);
            testCase.SetPropertyValue(GaugeProjectRoot, properties.ProjectRoot);
            testCase.SetPropertyValue(ScenarioIndex, scenarioIndex);

            logger.SendMessage(TestMessageLevel.Informational,
                string.Format("Discovered scenario: {0}", testCase.DisplayName));

            testCase.Traits.Add("Spec", spec.SpecHeading);

            foreach (var tag in scenario.TagsList.Union(spec.TagsList))
            {
                testCase.Traits.Add("Tag", tag);
            }
            return testCase;
        }
    }
}