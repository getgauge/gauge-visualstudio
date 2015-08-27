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
using Gauge.VisualStudio.Exceptions;

namespace Gauge.VisualStudio.Models
{
    public class Concept
    {
        private readonly EnvDTE.Project _project;

        public Concept(EnvDTE.Project project)
        {
            _project = project;
        }

        public Concept() 
            : this(GaugePackage.DTE.ActiveDocument.ProjectItem.ContainingProject)
        {
        }

        public string StepValue { get; set; }
        public string FilePath { get; set; }
        public int LineNumber { get; set; }

        public IEnumerable<Concept> GetAllConcepts()
        {
            if (_project == null)
            {
                return Enumerable.Empty<Concept>();
            }
            try
            {
                var gaugeApiConnection = GaugeDaemonHelper.GetApiConnectionFor(_project);
                var conceptsRequest = GetAllConceptsRequest.DefaultInstance;
                var apiMessage = APIMessage.CreateBuilder()
                    .SetMessageId(GenerateMessageId())
                    .SetMessageType(APIMessage.Types.APIMessageType.GetAllConceptsRequest)
                    .SetAllConceptsRequest(conceptsRequest)
                    .Build();

                var bytes = gaugeApiConnection.WriteAndReadApiMessage(apiMessage);

                return bytes.AllConceptsResponse.ConceptsList.Select(info => new Concept { StepValue = info.StepValue.ParameterizedStepValue, FilePath = info.Filepath, LineNumber = info.LineNumber });

            }
            catch (GaugeApiInitializationException)
            {
                return Enumerable.Empty<Concept>();
            }
        }

        private static long GenerateMessageId()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        public Concept Search(string lineText)
        {
            try
            {
                return GetAllConcepts().FirstOrDefault(
                            concept => string.Compare(Step.GetParsedStepValueFromInput(concept.StepValue), Step.GetParsedStepValueFromInput(lineText)) == 0);
            }
            catch
            {
                return null;                
            }
        }
    }
}