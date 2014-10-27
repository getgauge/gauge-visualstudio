using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using main;

namespace Gauge.VisualStudio.AutoComplete
{
    public class Steps
    {
        public static IEnumerable<string> GetAll()
        {
            var gaugeApiConnection = GaugeDTEProvider.GetApiConnectionForActiveDocument();
            Debug.WriteLine("requesting {0}", GaugeDTEProvider.DTE.ActiveDocument.ProjectItem.Name);
            var stepsRequest = GetAllStepsRequest.DefaultInstance;
            var apiMessage = APIMessage.CreateBuilder()
                .SetMessageId(GenerateMessageId())
                .SetMessageType(APIMessage.Types.APIMessageType.GetAllStepsRequest)
                .SetAllStepsRequest(stepsRequest)
                .Build();

            var bytes = gaugeApiConnection.WriteAndReadApiMessage(apiMessage);

            return bytes.AllStepsResponse.AllStepsList.Select(x => x.ParameterizedStepValue);
        }

        private static long GenerateMessageId()
        {
            return DateTime.Now.Ticks/TimeSpan.TicksPerMillisecond;
        }
    }
}