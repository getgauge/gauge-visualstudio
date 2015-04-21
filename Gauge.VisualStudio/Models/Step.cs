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
using System.Text.RegularExpressions;
using EnvDTE;
using main;
using Microsoft.VisualStudio.Text;

namespace Gauge.VisualStudio.Models
{
    public class Step
    {
        private static IList<ProtoStepValue> _allSteps;
        private static IEnumerable<Implementation> _gaugeImplementations;

        public static IEnumerable<string> GetAll()
        {
            return GetAllStepsFromGauge().Select(x => x.ParameterizedStepValue);
        }

        public static string GetParsedStepValue(ITextSnapshotLine input)
        {
            var stepValueFromInput = GetStepValueFromInput(GetStepText(input));
            return GetAllStepsFromGauge().First(value => value.StepValue == stepValueFromInput)
                   .ParameterizedStepValue;
        }

        public static CodeFunction GetStepImplementation(ITextSnapshotLine line, EnvDTE.Project containingProject = null)
        {
            if (containingProject==null)
            {
                containingProject = GaugeDTEProvider.DTE.ActiveDocument.ProjectItem.ContainingProject;
            }

            var lineText = GetStepText(line);

            _gaugeImplementations = _gaugeImplementations ?? Project.GetGaugeImplementations(containingProject);
            var gaugeImplementation = _gaugeImplementations.FirstOrDefault(implementation => implementation.ContainsFor(lineText));
            return gaugeImplementation == null ? null : gaugeImplementation.Function;
        }

        public static string GetStepText(ITextSnapshotLine line)
        {
            var originalText = line.GetText();
            var lineText = originalText.Replace('*', ' ').Trim();

            //if next line is a table then change the last word of the step to take in a special param
            if (IsTable(line))
                lineText = string.Format("{0} {{}}", lineText);
            return lineText;
        }

        public static bool IsTable(ITextSnapshotLine line)
        {
            var nextLineText = NextLineText(line);
            var tableRegex = new Regex(@"[ ]*\|[\w ]+\|", RegexOptions.Compiled);
            return tableRegex.IsMatch(nextLineText);
        }

        public static void Refresh()
        {
            try
            {
                _allSteps = GetAllStepsFromGauge();
                _gaugeImplementations = Project.GetGaugeImplementations(GaugeDTEProvider.DTE.ActiveDocument.ProjectItem.ContainingProject);

            }
            catch
            {
                // happens when project closes, and saves file on close. Ignore the refresh.
            }
        }

        private static IList<ProtoStepValue> GetAllStepsFromGauge()
        {
            var gaugeApiConnection = GaugeDTEProvider.GetApiConnectionForActiveDocument();
            var stepsRequest = GetAllStepsRequest.DefaultInstance;
            var apiMessage = APIMessage.CreateBuilder()
                .SetMessageId(GenerateMessageId())
                .SetMessageType(APIMessage.Types.APIMessageType.GetAllStepsRequest)
                .SetAllStepsRequest(stepsRequest)
                .Build();

            var bytes = gaugeApiConnection.WriteAndReadApiMessage(apiMessage);
            return bytes.AllStepsResponse.AllStepsList;
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

        private static long GenerateMessageId()
        {
            return DateTime.Now.Ticks/TimeSpan.TicksPerMillisecond;
        }

        internal static string GetStepValueFromInput(string input)
        {
            var stepRegex = new Regex(@"""([^""]*)""|\<([^\>]*)\>", RegexOptions.Compiled);
            return stepRegex.Replace(input, "{}");
        }


        // TODO : Use this when there is an implementation to listen to VS solution events.
        private static IEnumerable<ProtoStepValue> GetAllSteps(bool forceCacheUpdate=false)
        {
            if (forceCacheUpdate)
            {
                _allSteps = GetAllStepsFromGauge();
            }
            _allSteps = _allSteps ?? GetAllStepsFromGauge();
            return _allSteps;
        }
    }
}