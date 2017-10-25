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
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace Gauge.VisualStudio.TestAdapter
{
    [ExtensionUri(ExecutorUriString)]
    public class TestExecutor : ITestExecutor
    {
        public const string ExecutorUriString = "executor://gaugespecexecutor/v1";
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);
        private IGaugeRunner _gaugeRunner;

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            _gaugeRunner = new GaugeRunner(tests, runContext.IsBeingDebugged, frameworkHandle);
            _gaugeRunner.Run();
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var gaugeTestRunSettingsService =
                runContext.RunSettings.GetSettings(GaugeTestRunSettings.SettingsName) as IGaugeTestRunSettingsService;
            var testCases =
                TestDiscoverer.GetSpecs(gaugeTestRunSettingsService.Settings, null, sources, frameworkHandle);
            RunTests(testCases, runContext, frameworkHandle);
        }

        public void Cancel()
        {
            _gaugeRunner?.Cancel();
        }
    }
}