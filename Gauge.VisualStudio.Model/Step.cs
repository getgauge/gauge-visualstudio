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
using Gauge.VisualStudio.Core.Exceptions;
using Microsoft.VisualStudio.Text;

namespace Gauge.VisualStudio.Model
{
    public interface IStep
    {
        ITextSnapshotLine ContainingLine { get; set; }
        string Text { get; set; }
        List<string> Parameters { get; set; }
        bool HasInlineTable { get; }
        IEnumerable<string> GetAll();
        string GetParameterizedStepValue(ITextSnapshotLine input);
    }

    public class Step : IStep
    {
        public ITextSnapshotLine ContainingLine { get; set; }

        public string Text { get; set; }

        public List<string> Parameters { get; set; }

        private readonly EnvDTE.Project _project;

        public Step(EnvDTE.Project project)
        {
            _project = project;
        }

        public Step (EnvDTE.Project project, ITextSnapshotLine inputLine) : this(project)
        {
            ContainingLine = inputLine;
            var stepValueFromInput = GetStepValueFromInput(_project, GetStepText(inputLine));

            if (stepValueFromInput == null) 
                return;

            Text = stepValueFromInput.ParameterizedStepValue;
            Parameters = stepValueFromInput.ParametersList.ToList();
        }

        public bool HasInlineTable
        {
            get { return CheckForInlineTable(ContainingLine); }
        }

        public IEnumerable<string> GetAll()
        {
            return GetAllStepsFromGauge(_project).Select(x => x.ParameterizedStepValue);
        }

        public string GetParameterizedStepValue(ITextSnapshotLine input)
        {
            var stepValueFromInput = GetStepValueFromInput(_project, GetStepText(input));
            return stepValueFromInput == null ? string.Empty : stepValueFromInput.ParameterizedStepValue;
        }

        public static string GetStepText(ITextSnapshotLine line)
        {
            var originalText = line.GetText();
            var match = Parser.StepRegex.Match(originalText);
            var stepText = match.Groups["stepText"].Value.Trim();

            return CheckForInlineTable(line) ? string.Concat(stepText, " <table>") : stepText;
        }

        private static bool CheckForInlineTable(ITextSnapshotLine line)
        {
            if (line==null)
            {
                return false;
            }
            var nextLineText = NextLineText(line);
            return Parser.TableRegex.IsMatch(nextLineText);
        }

        private static IEnumerable<ProtoStepValue> GetAllStepsFromGauge(EnvDTE.Project project)
        {
            try
            {
                var gaugeApiConnection = GaugeService.GetApiConnectionFor(project);
                var stepsRequest = GetAllStepsRequest.DefaultInstance;
                var apiMessage = APIMessage.CreateBuilder()
                    .SetMessageId(GenerateMessageId())
                    .SetMessageType(APIMessage.Types.APIMessageType.GetAllStepsRequest)
                    .SetAllStepsRequest(stepsRequest)
                    .Build();

                var bytes = gaugeApiConnection.WriteAndReadApiMessage(apiMessage);
                return bytes.AllStepsResponse.AllStepsList;

            }
            catch (GaugeApiInitializationException)
            {
                return Enumerable.Empty<ProtoStepValue>();
            }
        }

        private static string NextLineText(ITextSnapshotLine currentLine)
        {
            ITextSnapshotLine nextLine;
            string nextLineText;
            try
            {
                nextLine = currentLine.Snapshot.GetLineFromLineNumber(currentLine.LineNumber + 1);
                nextLineText = nextLine.GetText();
            }
            catch
            {
                return string.Empty;
            }
            return nextLineText.Trim() == string.Empty && currentLine.LineNumber < currentLine.Snapshot.LineCount ? NextLineText(nextLine) : nextLineText;
        }

        public static long GenerateMessageId()
        {
            return DateTime.Now.Ticks/TimeSpan.TicksPerMillisecond;
        }

        internal static string GetParsedStepValueFromInput(EnvDTE.Project project, string input)
        {
            var stepValueFromInput = GetStepValueFromInput(project, input);
            return stepValueFromInput == null ? string.Empty : stepValueFromInput.StepValue;
        }

        public static string GetFindRegex(EnvDTE.Project project, string input)
        {
            var parsedValue = GetParsedStepValueFromInput(project, input);
            parsedValue = parsedValue.Replace("* ", "");
            return string.Format(@"^(\*[ |\t]*|[ |\t]*\[Step\(""){0}(""\)\])?\r", parsedValue.Replace("{}", "((<|\").+(>|\"))"));
        }

        private static ProtoStepValue GetStepValueFromInput(EnvDTE.Project project, string input)
        {
            try
            {
                var gaugeApiConnection = GaugeService.GetApiConnectionFor(project);
                var stepsRequest = GetStepValueRequest.CreateBuilder().SetStepText(input).Build();
                var apiMessage = APIMessage.CreateBuilder()
                    .SetMessageId(GenerateMessageId())
                    .SetMessageType(APIMessage.Types.APIMessageType.GetStepValueRequest)
                    .SetStepValueRequest(stepsRequest)
                    .Build();

                var bytes = gaugeApiConnection.WriteAndReadApiMessage(apiMessage);
                return bytes.StepValueResponse.StepValue;
            }
            catch (GaugeApiInitializationException)
            {
                return default(ProtoStepValue);
            }
        }
    }
}