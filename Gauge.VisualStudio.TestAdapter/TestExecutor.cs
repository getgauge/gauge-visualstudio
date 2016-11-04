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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace Gauge.VisualStudio.TestAdapter
{
    [ExtensionUri(ExecutorUriString)]
    public class TestExecutor : ITestExecutor
    {
        public const string ExecutorUriString = "executor://gaugespecexecutor/v1";
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);
        private bool _cancelled;
        readonly GaugeRunner _gaugeRunner = new GaugeRunner();

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var testSuites = new Dictionary<int, List<TestCase>>();
            _cancelled = false;
            foreach (var testCase in tests)
            {
                if (_cancelled) break;
                var port = testCase.GetPropertyValue(TestDiscoverer.GaugeApiV2Port, -1);
                if (!testSuites.ContainsKey(port))
                {
                    testSuites.Add(port, new List<TestCase>());
                }
                testSuites[port].Add(testCase);
            }
            Func<KeyValuePair<int, List<TestCase>>, Task> selector;
            if (runContext.IsBeingDebugged)
            {
                selector = suite => _gaugeRunner.Debug(suite.Value, suite.Key, frameworkHandle);
            }
            else
            {
                selector = suite => _gaugeRunner.Run(suite.Value, suite.Key, frameworkHandle);
            }
            var tasks = testSuites.Select(selector);
            Task.WaitAll(tasks.ToArray());
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var gaugeTestRunSettingsService = runContext.RunSettings.GetSettings(GaugeTestRunSettings.SettingsName) as GaugeTestRunSettingsService;
            var testCases = TestDiscoverer.GetSpecs(gaugeTestRunSettingsService.Settings, null, sources, frameworkHandle);
            RunTests(testCases, runContext, frameworkHandle);
        }

        public void Cancel()
        {
            _cancelled = true;
        }
    }
}