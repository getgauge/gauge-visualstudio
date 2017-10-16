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
using Gauge.Messages;
using Gauge.VisualStudio.Core;
using Gauge.VisualStudio.Core.Loggers;

namespace Gauge.VisualStudio.Model
{
    public class Concept
    {
        private readonly EnvDTE.Project _project;

        public Concept(EnvDTE.Project project)
        {
            _project = project;
        }

        public string StepValue { get; set; }
        public string FilePath { get; set; }
        public int LineNumber { get; set; }

        public IEnumerable<Concept> GetAllConcepts()
        {
            if (_project == null)
            {
                OutputPaneLogger.Error("Error occurred GetAllConcepts: _project is null");
                return Enumerable.Empty<Concept>();
            }
            var gaugeApiConnection = GaugeService.Instance.GetApiConnectionFor(_project);
            if (gaugeApiConnection == null)
            {
                OutputPaneLogger.Error("Error occurred GetAllConcepts: apiConnection is null");
                return Enumerable.Empty<Concept>();
            }
            var conceptsRequest = new GetAllConceptsRequest();
            var apiMessage = new APIMessage
            {
                MessageId = GenerateMessageId(),
                MessageType = APIMessage.Types.APIMessageType.GetAllConceptsRequest,
                AllConceptsRequest = conceptsRequest
            };

            var bytes = gaugeApiConnection.WriteAndReadApiMessage(apiMessage);

            return bytes.AllConceptsResponse.Concepts.Select(info => new Concept(_project)
            {
                StepValue = info.StepValue.ParameterizedStepValue,
                FilePath = info.Filepath,
                LineNumber = info.LineNumber
            });
        }

        private static long GenerateMessageId()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public Concept Search(string lineText)
        {
            try
            {
                var gaugeServiceClient = new GaugeServiceClient();
                return GetAllConcepts().FirstOrDefault(
                    concept => string.CompareOrdinal(
                                   gaugeServiceClient.GetParsedStepValueFromInput(_project, concept.StepValue),
                                   gaugeServiceClient.GetParsedStepValueFromInput(_project, lineText)) == 0);
            }
            catch
            {
                return null;
            }
        }
    }
}