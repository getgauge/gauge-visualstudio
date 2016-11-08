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
using System.Diagnostics;
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
            GrpcEnvironment.Initialize();
            var channel = new Channel("localhost", port);
            var executionClient = new Execution.ExecutionClient(channel);

            var executionRequestBuilder = ExecutionRequest.CreateBuilder().SetDebug(isBeingDebugged);

            var scenarios = testCases.ToDictionary(
                test => string.Format("{0}:{1}", test.CodeFilePath, test.LineNumber), test => test);

            executionRequestBuilder.AddRangeSpecs(scenarios.Keys);

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

                    if (!executionResponse.HasType) continue;
                    switch (executionResponse.Type)
                    {
                        case ExecutionResponse.Types.Type.ScenarioStart:
                        {
                            var testCase = scenarios[executionResponse.ID];
                            frameworkHandle.RecordStart(testCase);
                            frameworkHandle.SendMessage(TestMessageLevel.Informational,
                                string.Format("Executing Test: {0}", testCase));
                            break;
                        }
                        case ExecutionResponse.Types.Type.ScenarioEnd:
                        {
                            var testCase = scenarios[executionResponse.ID];
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
                    }
                }
                if (isBeingDebugged)
                {
                    DTEHelper.DetachAllProcess();
                }
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
