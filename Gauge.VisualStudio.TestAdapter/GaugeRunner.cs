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
using System.Diagnostics;
using Gauge.VisualStudio.Core;
using Gauge.VisualStudio.Core.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Gauge.VisualStudio.TestAdapter
{
    public class GaugeRunner
    {
        public void Run(TestCase testCase, bool isBeingDebugged, IFrameworkHandle frameworkHandle)
        {
            var result = new TestResult(testCase);
            frameworkHandle.RecordStart(testCase);
            frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Executing Test: {testCase}");
            var projectRoot = testCase.GetPropertyValue(TestDiscoverer.GaugeProjectRoot, string.Empty);
            var scenarioIdentifier = testCase.GetPropertyValue(TestDiscoverer.ScenarioIdentifier, -1);
            try
            {

                var gaugeCustomBuildPath = testCase.GetPropertyValue(TestDiscoverer.GaugeCustomBuildPath, string.Empty);
                var p = GaugeProcess.ForExecution(projectRoot, testCase.Source, scenarioIdentifier, gaugeCustomBuildPath, isBeingDebugged);
                frameworkHandle.SendMessage(TestMessageLevel.Informational,
                    $"Invoking : gauge.exe {p}");
                p.Start();

                if (isBeingDebugged)
                {
                    DTEHelper.AttachToProcess(p.Id);
                    frameworkHandle.SendMessage(TestMessageLevel.Informational,
                        $"Attaching to ProcessID {p.Id}");
                }
                var output = p.StandardOutput.ReadToEnd();
                var error = p.StandardError.ReadToEnd();

                p.WaitForExit();

                result.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, output));

                if (p.ExitCode == 0)
                {
                    result.Outcome = TestOutcome.Passed;
                }
                else
                {
                    result.ErrorMessage = error;
                    result.Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory, error));
                    result.Outcome = TestOutcome.Failed;
                }
            }
            catch (Exception ex)
            {
                result.Outcome = TestOutcome.Failed;
                result.ErrorMessage = string.Format("{0}\n{1}", ex.Message, ex.StackTrace);
            }
            frameworkHandle.RecordResult(result);
            frameworkHandle.RecordEnd(testCase, result.Outcome);
        }
    }
}