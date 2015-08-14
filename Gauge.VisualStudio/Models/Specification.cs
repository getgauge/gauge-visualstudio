using System.Collections.Generic;
using Gauge.Messages;

namespace Gauge.VisualStudio.Models
{
    public class Specification
    {
        public static List<ProtoSpec> GetAllSpecsFromGauge()
        {
            var specifications = new List<ProtoSpec>();
            foreach (var apiConnection in GaugeDTEProvider.GetAllApiConnections())
            {
                var specsRequest = GetAllSpecsRequest.DefaultInstance;
                var apiMessage = APIMessage.CreateBuilder()
                    .SetMessageId(Step.GenerateMessageId())
                    .SetMessageType(APIMessage.Types.APIMessageType.GetAllSpecsRequest)
                    .SetAllSpecsRequest(specsRequest)
                    .Build();

                var bytes = apiConnection.WriteAndReadApiMessage(apiMessage);
                specifications.AddRange(bytes.AllSpecsResponse.SpecsList);
            }

            return specifications;
        }
    }
}