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
using Microsoft.VisualStudio.Text;

namespace Gauge.VisualStudio.Model
{
    public class Step : IStep
    {
        private readonly IGaugeServiceClient _gaugeServiceClient;

        private readonly IProject _project;

        public Step(IProject project, ITextSnapshotLine inputLine, IGaugeServiceClient gaugeServiceClient)
        {
            _project = project;
            _gaugeServiceClient = gaugeServiceClient;
            ContainingLine = inputLine;
            var stepValueFromInput = _gaugeServiceClient.GetStepValueFromInput(_project.VsProject, GetStepText(inputLine));

            if (stepValueFromInput == null)
                return;

            Text = stepValueFromInput.ParameterizedStepValue;
            Parameters = stepValueFromInput.Parameters.ToList();
        }

        public Step(IProject vsProject, ITextSnapshotLine inputLine) : this(vsProject, inputLine,
            new GaugeServiceClient())
        {
        }

        public ITextSnapshotLine ContainingLine { get; }

        public string Text { get; }

        public List<string> Parameters { get; }

        public bool HasInlineTable => CheckForInlineTable(ContainingLine);

        public IEnumerable<string> GetAll()
        {
            var implementedSteps = _project.GetAllStepsForCurrentProject().ToList();
            var parsedImplementations =
                implementedSteps.Select(s => _gaugeServiceClient.GetParsedStepValueFromInput(_project.VsProject, s));
            var unimplementedSteps = _gaugeServiceClient.GetAllStepsFromGauge(_project.VsProject)
                .Where(s => !parsedImplementations.Contains(s.StepValue))
                .Select(value => value.ParameterizedStepValue);
            return unimplementedSteps.Union(implementedSteps);
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
            if (line == null)
                return false;
            var nextLineText = NextLineText(line);
            return Parser.TableRegex.IsMatch(nextLineText);
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
            return nextLineText.Trim() == string.Empty && currentLine.LineNumber < currentLine.Snapshot.LineCount
                ? NextLineText(nextLine)
                : nextLineText;
        }
    }
}