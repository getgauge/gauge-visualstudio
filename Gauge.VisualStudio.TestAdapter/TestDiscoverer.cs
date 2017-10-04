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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gauge.Messages;
using Gauge.VisualStudio.Core.Helpers;
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
        public static readonly TestProperty GaugeCustomBuildPath = TestProperty.Register(
            "TestCase.GaugeCustomBuildPath",
            "Custom build path for Gauge project binaries.", typeof(string), typeof(TestCase));

        public static readonly TestProperty GaugeProjectRoot = TestProperty.Register("TestCase.GaugeProjectRoot",
            "GAUGE_PROJECT_ROOT value set in Gauge.", typeof(string), typeof(TestCase));

        public static readonly TestProperty GaugeApiV2Port = TestProperty.Register("TestCase.GaugeApiV2Port",
            "GAUGE_API_V2_Port value set in Gauge.", typeof(int), typeof(TestCase));

        public static readonly TestProperty ScenarioIdentifier = TestProperty.Register("TestCase.ScenarioIdentifier",
            "Scenario identifier in a given spec.", typeof(int), typeof(TestCase));

        public static readonly TestProperty DaemonProcessId = TestProperty.Register("TestCase.DaemonProcessId",
            "PID of the corresponding daemon process.", typeof(int), typeof(TestCase));

        public static readonly TestProperty TestCaseType = TestProperty.Register("TestCase.Type",
            "Type of the testcase object. Can be [hook, scenario].", typeof(string), typeof(TestCase));

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext,
            IMessageLogger logger,
            ITestCaseDiscoverySink discoverySink)
        {
            var settingsProvider = discoveryContext.RunSettings.GetSettings(GaugeTestRunSettings.SettingsName)
                as IGaugeTestRunSettingsService;
            GetSpecs(settingsProvider.Settings, discoverySink, sources, logger);
        }

        public static List<TestCase> GetSpecs(GaugeTestRunSettings testRunSettings,
            ITestCaseDiscoverySink discoverySink,
            IEnumerable<string> sources, IMessageLogger logger)
        {
            var gaugeProjectProperties = testRunSettings.ProjectsProperties;
            var props = string.Join(",", gaugeProjectProperties.Select(p =>
                string.Format("ProjectRoot: {0}, OutputPath: {1}, API Port: {2}, API V2 Port: {3}", p.ProjectRoot,
                    p.BuildOutputPath, p.ApiPort, p.ApiV2Port)));
            logger.SendMessage(TestMessageLevel.Informational,
                string.Format("Discover Scenarios started. Using : {0}", props));

            var testCases = new ConcurrentBag<TestCase>();
            var testSources = sources.Where(s => string.CompareOrdinal(s, "Suite") != 0);
            try
            {
                Parallel.ForEach(gaugeProjectProperties, properties =>
                {
                    var protoSpecs = Specification.GetAllSpecs(properties.ApiPort);

                    Parallel.ForEach(protoSpecs, spec =>
                    {
                        if (testSources.All(s => string.CompareOrdinal(s, spec.FileName) != 0))
                            return;

                        logger.SendMessage(TestMessageLevel.Informational,
                            string.Format("Adding test cases from : {0}", spec.FileName));
                        var scenarioIndex = 0;
                        var scenarios = spec.Items.Where(item => item.Scenario != null).Select(item => item.Scenario);

                        foreach (var scenario in scenarios)
                        {
                            var testCase = CreateTestCase(logger, spec, scenario, properties, scenarioIndex);
                            testCases.Add(testCase);

                            if (discoverySink != null)
                                discoverySink.SendTestCase(testCase);

                            scenarioIndex++;
                        }
                    });
                });
            }
            catch (Exception e)
            {
                logger.SendMessage(TestMessageLevel.Error, e.ToString());
            }
            return testCases.ToList();
        }

        private static TestCase CreateTestCase(IMessageLogger logger, ProtoSpec spec, ProtoScenario scenario,
            GaugeProjectProperties properties, int scenarioIndex)
        {
            var testCaseName = string.Format("{0}.{1}", spec.SpecHeading, scenario.ScenarioHeading);
            var testCase = new TestCase(testCaseName, TestExecutor.ExecutorUri, spec.FileName)
                {CodeFilePath = spec.FileName, DisplayName = scenario.ScenarioHeading};

            var scenarioIdentifier = GetScenarioIdentifier(scenarioIndex, scenario);
            testCase.LineNumber = scenarioIdentifier;

            testCase.SetPropertyValue(ScenarioIdentifier, scenarioIdentifier);
            testCase.SetPropertyValue(GaugeCustomBuildPath, properties.BuildOutputPath);
            testCase.SetPropertyValue(GaugeProjectRoot, properties.ProjectRoot);
            testCase.SetPropertyValue(GaugeApiV2Port, properties.ApiV2Port);
            testCase.SetPropertyValue(DaemonProcessId, properties.DaemonProcessId);
            testCase.SetPropertyValue(TestCaseType, "scenario");

            logger.SendMessage(TestMessageLevel.Informational,
                string.Format("Discovered scenario: {0}", testCase.DisplayName));

            testCase.Traits.Add("Spec", spec.SpecHeading);

            foreach (var tag in scenario.Tags.Union(spec.Tags))
                testCase.Traits.Add("Tag", tag);
            return testCase;
        }

        private static int GetScenarioIdentifier(int scenarioIndex, ProtoScenario scenario)
        {
            return scenario.Span != null ? (int) scenario.Span.Start : scenarioIndex;
        }
    }
}