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
using Gauge.VisualStudio.Core;
using Gauge.VisualStudio.Core.Exceptions;

namespace Gauge.VisualStudio.Model
{
    public class Specification
    {
        public static IEnumerable<string> GetAllSpecsFromGauge()
        {
            var specifications = new List<ProtoSpec>();
            try
            {
                foreach (var apiConnection in GaugeService.GetAllApiConnections())
                {
                    specifications.AddRange(GetSpecsFromGauge(apiConnection));
                }

                return specifications.Select(spec => spec.FileName).Distinct();

            }
            catch (GaugeApiInitializationException)
            {
                return Enumerable.Empty<string>();
            }
        }

        public static IEnumerable<ProtoSpec> GetAllSpecs(int apiPort)
        {
            var gaugeApiConnection = new GaugeApiConnection(new TcpClientWrapper(apiPort));
            return GetSpecsFromGauge(gaugeApiConnection);
        }

        private static IEnumerable<ProtoSpec> GetSpecsFromGauge(GaugeApiConnection apiConnection)
        {
            var specsRequest = SpecsRequest.DefaultInstance;
            var apiMessage = APIMessage.CreateBuilder()
                .SetMessageId(Step.GenerateMessageId())
                .SetMessageType(APIMessage.Types.APIMessageType.SpecsRequest)
                .SetSpecsRequest(specsRequest)
                .Build();

            var bytes = apiConnection.WriteAndReadApiMessage(apiMessage);

            var specs = bytes.SpecsResponse.DetailsList.Where(detail => detail.HasSpec).Select(detail => detail.Spec);
            return specs;
        }
    }
}