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
    public class GaugeRunner
    {
        // TODO:
        // - Add debug support via execution API
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
                foreach (var testCase in testCases)
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Informational, string.Format("{0} {1}",testCase.DisplayName, testCase.GetPropertyValue(TestDiscoverer.TestCaseType,string.Empty)));
                }
                GrpcEnvironment.Initialize();
                var channel = new Channel("localhost", port);
                var executionClient = new Execution.ExecutionClient(channel);

                var executionRequestBuilder = ExecutionRequest.CreateBuilder().SetDebug(isBeingDebugged);

                var beforeSuiteHook = testCases.First(test => test.DisplayName == "BeforeSuite" &&
                                                              string.CompareOrdinal(
                                                                  test.GetPropertyValue(TestDiscoverer.TestCaseType,
                                                                      string.Empty), "hook") == 0);
                var afterSuiteHook = testCases.First(test => test.DisplayName == "AfterSuite" &&
                                                             string.CompareOrdinal(
                                                                 test.GetPropertyValue(TestDiscoverer.TestCaseType,
                                                                     string.Empty), "hook") == 0);
                var beforeSpecHooks = testCases.Where(test => test.DisplayName == "BeforeSpec" &&
                                                              string.CompareOrdinal(
                                                                  test.GetPropertyValue(TestDiscoverer.TestCaseType,
                                                                      string.Empty), "hook") == 0)
                    .ToDictionary(test => test.CodeFilePath, test => test);
                var afterSpecHooks = testCases.Where(test => test.DisplayName == "AfterSpec" &&
                                                             string.CompareOrdinal(
                                                                 test.GetPropertyValue(TestDiscoverer.TestCaseType,
                                                                     string.Empty), "hook") == 0)
                    .ToDictionary(test => test.CodeFilePath, test => test);
                var scenariosMap = testCases.Where(
                    test =>
                        string.CompareOrdinal(test.GetPropertyValue(TestDiscoverer.TestCaseType, string.Empty),
                            "scenario") == 0)
                    .ToDictionary(test => string.Format("{0}:{1}", test.CodeFilePath, test.LineNumber), test => test);

                executionRequestBuilder.AddRangeSpecs(scenariosMap.Keys);

                using (var call = executionClient.execute(executionRequestBuilder.Build()))
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

                        frameworkHandle.SendMessage(TestMessageLevel.Informational,
                            string.Format("Received: {0}", executionResponse.Type));
                        if (!executionResponse.HasType)
                        {
                            continue;
                        }
                        switch (executionResponse.Type)
                        {
                            case ExecutionResponse.Types.Type.SuiteStart:
                            {
                                var testResult = new TestResult(beforeSuiteHook)
                                {
                                    Outcome = executionResponse.Result.HasStatus
                                        ? GetVSResult(executionResponse.Result.Status)
                                        : TestOutcome.None
                                };
                                frameworkHandle.RecordResult(testResult);
                                frameworkHandle.RecordEnd(beforeSuiteHook, testResult.Outcome);
                                break;
                            }
                            case ExecutionResponse.Types.Type.SuiteEnd:
                            {
                                var testResult = new TestResult(afterSuiteHook)
                                {
                                    Outcome = executionResponse.Result.HasStatus
                                        ? GetVSResult(executionResponse.Result.Status)
                                        : TestOutcome.None
                                };
                                frameworkHandle.RecordResult(testResult);
                                frameworkHandle.RecordEnd(afterSuiteHook, testResult.Outcome);
                                break;
                            }
                            case ExecutionResponse.Types.Type.SpecStart:
                            {
                                var spec = executionResponse.ID.Split(':')[0];
                                var testCase = beforeSpecHooks[spec];
                                var testResult = new TestResult(testCase)
                                {
                                    Outcome = executionResponse.Result.HasStatus
                                        ? GetVSResult(executionResponse.Result.Status)
                                        : TestOutcome.None
                                };
                                frameworkHandle.RecordResult(testResult);
                                frameworkHandle.RecordEnd(testCase, testResult.Outcome);
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
                                    Outcome = executionResponse.Result.HasStatus
                                        ? GetVSResult(executionResponse.Result.Status)
                                        : TestOutcome.None
                                };
                                frameworkHandle.RecordResult(result);
                                frameworkHandle.RecordEnd(testCase, result.Outcome);
                                break;
                            }
                            case ExecutionResponse.Types.Type.SpecEnd:
                            {
                                var spec = executionResponse.ID.Split(':')[0];
                                var testCase = afterSpecHooks[spec];
                                var testResult = new TestResult(testCase)
                                {
                                    Outcome = executionResponse.Result.HasStatus
                                        ? GetVSResult(executionResponse.Result.Status)
                                        : TestOutcome.None
                                };
                                frameworkHandle.RecordResult(testResult);
                                frameworkHandle.RecordEnd(testCase, testResult.Outcome);
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
                case Result.Types.Status.FAILED:
                    return TestOutcome.Failed;
                case Result.Types.Status.PASSED:
                    return TestOutcome.Passed;
                case Result.Types.Status.SKIPPED:
                    return TestOutcome.Skipped;
                default:
                    return TestOutcome.None;
            }
        }
    }
}
