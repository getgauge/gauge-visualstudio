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

using System.Collections.Generic;
using System.Linq;
using Gauge.CSharp.Core;
using Gauge.Messages;
using Gauge.VisualStudio.Helpers;

namespace Gauge.VisualStudio.Models
{
    public class Specification
    {
        public static IEnumerable<string> GetAllSpecsFromGauge()
        {
            var specifications = new List<ProtoSpec>();
            foreach (var apiConnection in GaugeDaemonHelper.GetAllApiConnections())
            {
                var specsList = GetSpecsFromGauge(apiConnection);
                specifications.AddRange(specsList);
            }

            return specifications.Select(spec => spec.FileName).Distinct();
        }

        public static IEnumerable<ProtoSpec> GetAllSpecs(IEnumerable<int> apiPorts)
        {
            return apiPorts.SelectMany(i =>
            {
                var gaugeApiConnection = new GaugeApiConnection(new TcpClientWrapper(i));
                return GetSpecsFromGauge(gaugeApiConnection);
            }).Distinct();
        }

        private static IEnumerable<ProtoSpec> GetSpecsFromGauge(GaugeApiConnection apiConnection)
        {
            var specsRequest = GetAllSpecsRequest.DefaultInstance;
            var apiMessage = APIMessage.CreateBuilder()
                .SetMessageId(Step.GenerateMessageId())
                .SetMessageType(APIMessage.Types.APIMessageType.GetAllSpecsRequest)
                .SetAllSpecsRequest(specsRequest)
                .Build();

            var bytes = apiConnection.WriteAndReadApiMessage(apiMessage);
            return bytes.AllSpecsResponse.SpecsList;
        }
    }
}