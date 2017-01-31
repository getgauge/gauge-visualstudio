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
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gauge.Messages;
using Gauge.VisualStudio.Core.Helpers;
using Grpc.Core;

namespace Gauge.VisualStudio.TestAdapter
{
    public class GaugeRunnerV2
    {
        // TODO:
        // - Read STDOUT / STDERR from response

        public async Task Debug(List<TestCase> testCases, int port, IFrameworkHandle frameworkHandle)
        {
            await Run(testCases, port, frameworkHandle, true);
        }

        public async Task Run(List<TestCase> testCases, int port, IFrameworkHandle frameworkHandle,
            bool isBeingDebugged = false)
        {
            try
            {
                var specsMap = new Dictionary<string, TestCase>();
                foreach (var testCase in testCases)
                {
                    if (!specsMap.ContainsKey(testCase.CodeFilePath))
                    {
                        specsMap.Add(testCase.CodeFilePath, testCase);
                    }
                    frameworkHandle.SendMessage(TestMessageLevel.Informational, string.Format("{0} {1}",testCase.DisplayName, testCase.GetPropertyValue(TestDiscoverer.TestCaseType,string.Empty)));
                }
                
                var channel = new Channel("localhost", port, ChannelCredentials.Insecure);
                var executionClient = new Execution.ExecutionClient(channel);

                var executionRequest = new ExecutionRequest(){Debug = isBeingDebugged};

                var scenariosMap = testCases.Where(test => string.CompareOrdinal(test.GetPropertyValue(TestDiscoverer.TestCaseType, string.Empty), "scenario") == 0)
                    .ToDictionary(test => string.Format("{0}:{1}", test.CodeFilePath, test.LineNumber), test => test);
                executionRequest.Specs.AddRange(scenariosMap.Keys);

                using (var call = executionClient.execute(executionRequest))
                {
                    var processId = testCases.First().GetPropertyValue(TestDiscoverer.DaemonProcessId, -1);
                    if (isBeingDebugged)
                    {
                        try
                        {
                            DTEHelper.AttachToProcess(processId);
                        }
                        catch (Exception e)
                        {
                            frameworkHandle.SendMessage(TestMessageLevel.Error,
                                string.Format("Unable to attach debugger. Details:\n{0}", e));
                        }
                    }
                    while (await call.ResponseStream.MoveNext())
                    {
                        var executionResponse = call.ResponseStream.Current;

                        if (executionResponse.Result != null && executionResponse.Result.Status == Result.Types.Status.Failed)
                        {
                            var errors = string.Join(Environment.NewLine, executionResponse.Result.Errors.Select(
                                error => string.Format("{0}\n{1}", error.ErrorMessage, error.StackTrace)));
                            frameworkHandle.SendMessage(TestMessageLevel.Error, errors);
                            return;
                        }

                        Action<string, string> propogateIfSuiteFailure = (hook, spec) =>
                        {
                            if (executionResponse.Result == null) return;
                            var error = string.Empty;
                            if (executionResponse.Result.BeforeHookFailure != null)
                            {
                                var failure = executionResponse.Result.BeforeHookFailure;
                                error = string.Format("[Before{0} Failure] : {1}\n{2}\n", hook, failure.ErrorMessage, failure.StackTrace);
                            }
                            if (executionResponse.Result.AfterHookFailure != null)
                            {
                                var failure = executionResponse.Result.AfterHookFailure;
                                error = string.Format("[Before{0} Failure] : {1}\n{2}\n", hook, failure.ErrorMessage, failure.StackTrace);
                            }
                            if (string.IsNullOrEmpty(error))
                            {
                                return;
                            }
                            foreach (var testCase in testCases)
                            {
                                if (spec == null || testCase.CodeFilePath.Contains(spec))
                                {
                                    var testResult = new TestResult(testCase) {Outcome = TestOutcome.Failed};
                                    testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory, error));
                                    frameworkHandle.RecordResult(testResult);
                                    frameworkHandle.RecordEnd(testCase, testResult.Outcome);
                                }
                            }
                        };

                        if (executionResponse.Type == null)
                        {
                            continue;
                        }
                        switch (executionResponse.Type)
                        {
                            case ExecutionResponse.Types.Type.ErrorResult:
                            {
                                if (executionResponse.Result != null)
                                {
                                    var errors = string.Join(Environment.NewLine, executionResponse.Result.Errors.Select( error => string.Format("{0}\n{1}", error.ErrorMessage, error.StackTrace)));
                                    frameworkHandle.SendMessage(TestMessageLevel.Error, errors);
                                }
                                else
                                {
                                    frameworkHandle.SendMessage(TestMessageLevel.Error, "An error occurred during execution, details unavailable. Check Gauge logs.");
                                }
                                break;
                            }
                            case ExecutionResponse.Types.Type.SuiteEnd:
                            {
                                propogateIfSuiteFailure("Suite", null);
                                break;
                            }
                            case ExecutionResponse.Types.Type.SpecEnd:
                            {
                                var executionResponseId = executionResponse.ID;
                                var spec = executionResponseId.Split(':')[0];
                                propogateIfSuiteFailure("Spec", spec);
                                break;
                            }
                            case ExecutionResponse.Types.Type.ScenarioStart:
                            {
                                var testCase = scenariosMap[executionResponse.ID];
                                frameworkHandle.RecordStart(testCase);
                                frameworkHandle.SendMessage(TestMessageLevel.Informational,
                                    string.Format("Executing Test: {0}", testCase));
                                break;
                            }
                            case ExecutionResponse.Types.Type.ScenarioEnd:
                            {
                                var testCase = scenariosMap[executionResponse.ID];
                                var result = new TestResult(testCase)
                                {
                                    Outcome = executionResponse.Result.Status != null
                                        ? GetVSResult(executionResponse.Result.Status)
                                        : TestOutcome.None
                                };
                                frameworkHandle.RecordResult(result);
                                frameworkHandle.RecordEnd(testCase, result.Outcome);
                                break;
                            }
                        }
                    }
                    if (isBeingDebugged)
                    {
                        DTEHelper.DetachAllProcess();
                    }
                }
            }
            catch (Exception ex)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Error, ex.ToString());
            }
        }

        private static TestOutcome GetVSResult(Result.Types.Status status)
        {
            switch (status)
            {
                case Result.Types.Status.Failed:
                    return TestOutcome.Failed;
                case Result.Types.Status.Passed:
                    return TestOutcome.Passed;
                case Result.Types.Status.Skipped:
                    return TestOutcome.Skipped;
                default:
                    return TestOutcome.None;
            }
        }
    }
}
