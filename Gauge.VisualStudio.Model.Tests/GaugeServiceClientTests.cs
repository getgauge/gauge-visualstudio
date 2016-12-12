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
using FakeItEasy;
using Gauge.CSharp.Core;
using Gauge.Messages;
using Gauge.VisualStudio.Core;
using Gauge.VisualStudio.Core.Exceptions;
using NUnit.Framework;

namespace Gauge.VisualStudio.Model.Tests
{
    [TestFixture]
    public class GaugeServiceClientTests
    {
        [Test]
        public void ShouldGetParsedValueFromGauge()
        {
            const string expected = "foo message with {}";
            const string input = "foo message with <parameter>";

            var gaugeService = A.Fake<IGaugeService>();
            var project = A.Fake<EnvDTE.Project>();
            var gaugeApiConnection = A.Fake<IGaugeApiConnection>();
            var response = APIMessage.CreateBuilder()
                .SetMessageType(APIMessage.Types.APIMessageType.GetStepValueResponse)
                .SetMessageId(0)
                .SetStepValueResponse(GetStepValueResponse.CreateBuilder()
                    .SetStepValue(ProtoStepValue.CreateBuilder()
                    .SetParameterizedStepValue(input)
                        .SetStepValue(expected)))
                .Build();
            A.CallTo(() => gaugeApiConnection.WriteAndReadApiMessage(A<APIMessage>._))
                .Returns(response);
            A.CallTo(() => gaugeService.GetApiConnectionFor(project)).Returns(gaugeApiConnection);
            var gaugeServiceClient = new GaugeServiceClient(gaugeService);

            var actual = gaugeServiceClient.GetParsedStepValueFromInput(project, input);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void ShouldGetParsedValueAsEmptyWhenApiNotInitialized()
        {
            const string input = "foo message with <parameter>";

            var gaugeService = A.Fake<IGaugeService>();
            var project = A.Fake<EnvDTE.Project>();
            var gaugeApiConnection = A.Fake<IGaugeApiConnection>();
            A.CallTo(() => gaugeApiConnection.WriteAndReadApiMessage(A<APIMessage>._))
                .Throws(new GaugeApiInitializationException());
            A.CallTo(() => gaugeService.GetApiConnectionFor(project)).Returns(gaugeApiConnection);
            var gaugeServiceClient = new GaugeServiceClient(gaugeService);

            var actual = gaugeServiceClient.GetParsedStepValueFromInput(project, input);

            Assert.AreEqual(string.Empty, actual);
        }

        [Test, TestCaseSource("MatchTestCases")]
        public void ShouldLocateReferencesUsingRegex(string input, string parsedInput, string match)
        {
            var gaugeService = A.Fake<IGaugeService>();
            var project = A.Fake<EnvDTE.Project>();
            var gaugeServiceClient = GetGaugeServiceClient(input, parsedInput, gaugeService, project);

            var findRegex = gaugeServiceClient.GetFindRegex(project, input);

            StringAssert.IsMatch(findRegex, match);
        }

        [Test, TestCaseSource("MatchNegativeTestCases")]
        public void ShouldNotLocateFalseMatchesUsingRegex(string input, string parsedInput, string match)
        {
            var gaugeService = A.Fake<IGaugeService>();
            var project = A.Fake<EnvDTE.Project>();
            var gaugeServiceClient = GetGaugeServiceClient(input, parsedInput, gaugeService, project);

            var findRegex = gaugeServiceClient.GetFindRegex(project, input);

            StringAssert.DoesNotMatch(findRegex, match);
        }

        private static IEnumerable<TestCaseData> MatchTestCases
        {
            get
            {
                yield return new TestCaseData(@"Say ""hello"" to ""gauge""", @"Say {} to {}",
                        @"* Say ""hello"" to ""gauge""" + Environment.NewLine);
                yield return new TestCaseData(@"Say ""hello"" to ""gauge""", @"Say {} to {}",
                        @"* Say <what> to <whom>" + Environment.NewLine);
                yield return new TestCaseData(@"Say ""hello"" to ""gauge""", @"Say {} to {}",
                        @"  [Step(""Say <what> to <whom>"")]" + Environment.NewLine);

                yield return new TestCaseData(@"Step that takes a <table>", @"Step that takes a",
                        @"  [Step(""Step that takes a <table>"")]" + Environment.NewLine);
                yield return new TestCaseData(@"Step that takes a <table>", @"Step that takes a",
                        @"*  Step that takes a" + Environment.NewLine +
                        @"    |foo|bar|" + Environment.NewLine);
                yield return new TestCaseData(@"Step that takes a <table>", @"Step that takes a",
                        @"*  Step that takes a " + Environment.NewLine +
                        @"    |foo|bar|" + Environment.NewLine);
            }
        }

        private static IEnumerable<TestCaseData> MatchNegativeTestCases
        {
            get
            {
                //negative
                yield return new TestCaseData(@"Say ""hello"" to ""gauge""", @"Say {} to {}",
                        @"* Say <what> to <whom> at foo" + Environment.NewLine);
            }
        }

        private static GaugeServiceClient GetGaugeServiceClient(string input, string parsedInput, IGaugeService gaugeService,
            EnvDTE.Project project)
        {
            var gaugeServiceClient = new GaugeServiceClient(gaugeService);
            var gaugeApiConnection = A.Fake<IGaugeApiConnection>();
            var response = APIMessage.CreateBuilder()
                .SetMessageType(APIMessage.Types.APIMessageType.GetStepValueResponse)
                .SetMessageId(0)
                .SetStepValueResponse(GetStepValueResponse.CreateBuilder()
                    .SetStepValue(ProtoStepValue.CreateBuilder()
                        .SetParameterizedStepValue(input)
                        .SetStepValue(parsedInput)))
                .Build();
            A.CallTo(() => gaugeApiConnection.WriteAndReadApiMessage(A<APIMessage>._))
                .Returns(response);
            A.CallTo(() => gaugeService.GetApiConnectionFor(project)).Returns(gaugeApiConnection);
            return gaugeServiceClient;
        }
    }
}