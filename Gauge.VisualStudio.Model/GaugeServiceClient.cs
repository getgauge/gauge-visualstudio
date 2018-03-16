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
using System.Linq;
using Gauge.CSharp.Core;
using Gauge.Messages;
using Gauge.VisualStudio.Core;

namespace Gauge.VisualStudio.Model
{
    public class GaugeServiceClient : IGaugeServiceClient
    {
        private readonly IGaugeService _gaugeService;

        public GaugeServiceClient(IGaugeService gaugeService)
        {
            _gaugeService = gaugeService;
        }

        public GaugeServiceClient() : this(GaugeService.Instance)
        {
        }

        public string GetParsedStepValueFromInput(EnvDTE.Project project, string input)
        {
            var stepValueFromInput = GetStepValueFromInput(project, input);
            return stepValueFromInput == null ? string.Empty : stepValueFromInput.StepValue;
        }

        public string GetFindRegex(EnvDTE.Project project, string input)
        {
            if (input.EndsWith(" <table>", StringComparison.Ordinal))
                input = input.Remove(input.LastIndexOf(" <table>", StringComparison.Ordinal));
            var parsedValue = GetParsedStepValueFromInput(project, input);
            parsedValue = parsedValue.Replace("* ", "");
            return $@"^(\*[ |\t]*|[ |\t]*\[Step\(""){
                    parsedValue.Replace("{}", "((<|\")(?!<table>).+(>|\"))")
                }\s*(((\r?\n\s*)+\|([\w ]+\|)+)|(<table>))?(""\)\])?\r?\n";
        }

        public ProtoStepValue GetStepValueFromInput(EnvDTE.Project project, string input)
        {
            var gaugeApiConnection = _gaugeService.GetApiConnectionFor(project);
            var stepsRequest = new GetStepValueRequest { StepText = input };
            var apiMessage = new APIMessage
            {
                MessageId = GenerateMessageId(),
                MessageType = APIMessage.Types.APIMessageType.GetStepValueRequest,
                StepValueRequest = stepsRequest
            };

            var bytes = gaugeApiConnection.WriteAndReadApiMessage(apiMessage);
            return bytes.StepValueResponse.StepValue;
        }

        public IEnumerable<ProtoStepValue> GetAllStepsFromGauge(EnvDTE.Project project)
        {
            var gaugeApiConnection = _gaugeService.GetApiConnectionFor(project);

            if (gaugeApiConnection == null)
                return Enumerable.Empty<ProtoStepValue>();
            var stepsRequest = new GetAllStepsRequest();
            var apiMessage = new APIMessage
            {
                MessageId = GenerateMessageId(),
                MessageType = APIMessage.Types.APIMessageType.GetAllStepsRequest,
                AllStepsRequest = stepsRequest
            };

            var bytes = gaugeApiConnection.WriteAndReadApiMessage(apiMessage);
            return bytes.AllStepsResponse.AllSteps;
        }

        public IEnumerable<ProtoSpec> GetSpecsFromGauge(IGaugeApiConnection apiConnection)
        {
            var specsRequest = new SpecsRequest();
            var apiMessage = new APIMessage
            {
                MessageId = GenerateMessageId(),
                MessageType = APIMessage.Types.APIMessageType.SpecsRequest,
                SpecsRequest = specsRequest
            };

            var bytes = apiConnection.WriteAndReadApiMessage(apiMessage);

            var specs = bytes.SpecsResponse.Details.Where(detail => detail.Spec != null).Select(detail => detail.Spec);
            return specs;
        }

        public static long GenerateMessageId()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public IEnumerable<ProtoSpec> GetSpecsFromGauge(int apiPort)
        {
            return GetSpecsFromGauge(new GaugeApiConnection(new TcpClientWrapper(apiPort)));
        }
    }
}