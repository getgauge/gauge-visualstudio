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

        public static readonly TestProperty DaemonProcessId = TestProperty.Register("TestCase.DaemonProcessId",
            "PID of the corresponding daemon process.", typeof(int), typeof(TestCase));

        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext,
            IMessageLogger logger,
            ITestCaseDiscoverySink discoverySink)
        {
            var settingsProvider = discoveryContext.RunSettings.GetSettings(GaugeTestRunSettings.SettingsName)
                as IGaugeTestRunSettingsService;
            if (settingsProvider == null)
            {
                logger.SendMessage(TestMessageLevel.Error, $"Unable to retrieve Test settings provider for {GaugeTestRunSettings.SettingsName}");
                return;
            }
            GetSpecs(settingsProvider.Settings, discoverySink, sources, logger);
        }

        public static List<TestCase> GetSpecs(GaugeTestRunSettings testRunSettings,
            ITestCaseDiscoverySink discoverySink,
            IEnumerable<string> sources, IMessageLogger logger)
        {
            var gaugeProjectProperties = testRunSettings.ProjectsProperties;
            var props = string.Join(",", gaugeProjectProperties.Select(p =>
                $"ProjectRoot: {p.ProjectRoot}, OutputPath: {p.BuildOutputPath}, API Port: {p.ApiPort}"));
            logger.SendMessage(TestMessageLevel.Informational,
                $"Discover Scenarios started. Using : {props}");

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

                        logger.SendMessage(TestMessageLevel.Informational, $"Adding test cases from : {spec.FileName}");
                        var scenarios = spec.Items.Where(item => item.Scenario != null).Select(item => item.Scenario);

                        foreach (var scenario in scenarios)
                        {
                            var testCase = CreateTestCase(logger, spec, scenario, properties);
                            testCases.Add(testCase);
                            discoverySink?.SendTestCase(testCase);
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
            GaugeProjectProperties properties)
        {
            var testCase = new TestCase(scenario.ScenarioHeading, TestExecutor.ExecutorUri, spec.SpecHeading)
            {
                CodeFilePath = spec.FileName,
                DisplayName = scenario.ScenarioHeading,
                LineNumber = (int) scenario.Span.Start
            };

            testCase.SetPropertyValue(GaugeCustomBuildPath, properties.BuildOutputPath);
            testCase.SetPropertyValue(GaugeProjectRoot, properties.ProjectRoot);
            testCase.SetPropertyValue(DaemonProcessId, properties.DaemonProcessId);

            logger.SendMessage(TestMessageLevel.Informational, $"Discovered scenario: {testCase.DisplayName}");

            testCase.Traits.Add("Spec", spec.SpecHeading);

            foreach (var tag in scenario.Tags.Union(spec.Tags))
                testCase.Traits.Add("Tag", tag);
            return testCase;
        }
    }
}