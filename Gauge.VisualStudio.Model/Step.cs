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
using System;
using Gauge.Messages;

namespace Gauge.VisualStudio.Model
{
    public class Step : IStep
    {
        private readonly IGaugeServiceClient _gaugeServiceClient;

        private readonly IProject _project;
        private Lazy<ProtoStepValue> _stepValueFromInput;
        private readonly Lazy<List<string>> _parameters;
        private Lazy<string> _text;

        public Step(IProject project, ITextSnapshotLine inputLine, IGaugeServiceClient gaugeServiceClient)
        {
            _project = project;
            _gaugeServiceClient = gaugeServiceClient;
            ContainingLine = inputLine;
            _stepValueFromInput = new Lazy<ProtoStepValue>(() => _gaugeServiceClient.GetStepValueFromInput(_project.VsProject, GetStepText(inputLine)));

            if (_stepValueFromInput == null)
                return;

            _text = new Lazy<string>(() => _stepValueFromInput.IsValueCreated ? _stepValueFromInput.Value.ParameterizedStepValue : null);
            _parameters = new Lazy<List<string>>(() => _stepValueFromInput.IsValueCreated ? _stepValueFromInput.Value.Parameters.ToList() : null);
        }

        public Step(IProject vsProject, ITextSnapshotLine inputLine) : this(vsProject, inputLine,
            new GaugeServiceClient())
        {
        }

        public ITextSnapshotLine ContainingLine { get; }

        public string Text => _text.Value;

        public List<string> Parameters => _parameters.Value;

        public bool HasInlineTable => CheckForInlineTable(ContainingLine);

        public static string GetStepText(ITextSnapshotLine line)
        {
            var originalText = line.GetText();
            var match = Parser.StepRegex.Match(originalText);
            var stepText = match.Groups["stepText"].Value.Trim();

            return CheckForInlineTable(line) ? string.Concat(stepText, " <table>") : stepText;
        }

        public static string GetStepValue(ITextSnapshotLine line)
        {
            return Parser.StepValueRegex.Replace(GetStepText(line), "{}");
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