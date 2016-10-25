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

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using System.Collections.Generic;
using System.Linq;
using Gauge.Messages;
using Grpc.Core;

namespace Gauge.VisualStudio.TestAdapter
{
    public class GaugeRunner
    {
        // TODO:
        // - Add debug support via execution API
        // - Read STDOUT / STDERR from response
        public async void Run(List<TestCase> testCases, int port, bool isBeingDebugged, IFrameworkHandle frameworkHandle)
        {
            var channel = new Channel("localhost", port);
            var executionClient = new Execution.ExecutionClient(channel);

            var executionRequestBuilder = ExecutionRequest.CreateBuilder();

            var scenarios = testCases.ToDictionary(
                test => string.Format("{0}:{1}", test.CodeFilePath, test.LineNumber), test => test);

            executionRequestBuilder.AddRangeSpecs(scenarios.Keys);
            using (var call = executionClient.execute(executionRequestBuilder.Build()))
            {
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
                            var result = new TestResult(testCase);
                            if (!executionResponse.Result.HasStatus)
                            {
                                result.Outcome=TestOutcome.None;
                            }
                            else
                            {
                                result.Outcome = GetVSResult(executionResponse.Result.Status);
                            }
                            frameworkHandle.RecordResult(result);
                            frameworkHandle.RecordEnd(testCase, result.Outcome);
                            break;
                        }
                    }
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
