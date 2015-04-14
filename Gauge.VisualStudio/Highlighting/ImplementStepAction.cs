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
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EnvDTE;
using Gauge.CSharp.Lib;
using Gauge.VisualStudio.Classification;
using Gauge.VisualStudio.Models;
using Gauge.VisualStudio.UI;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Project = Gauge.VisualStudio.Models.Project;

namespace Gauge.VisualStudio.Highlighting
{
    internal class ImplementStepAction : ISmartTagAction
    {
        private readonly ITrackingSpan _span;
        private readonly ITextSnapshot _snapshot;
        private string _upper;
        private readonly string _display;
        private bool _enabled = true;

        public ImplementStepAction(ITrackingSpan trackingSpan, UnimplementedStepTagger unimplementedStepTagger)
        {
            _span = trackingSpan;
            _snapshot = _span.TextBuffer.CurrentSnapshot;
            _upper = _span.GetText(_snapshot).ToUpper();
            _display = "Implement Step";
            Icon = new BitmapImage(new Uri("pack://application:,,,/Gauge.VisualStudio;component/assets/glyphs/step.png"));
        }

        public void Invoke()
        {
            var classPicker = new ClassPicker();
            var selectedClass = string.Empty;
            classPicker.ShowModal();
            selectedClass = classPicker.SelectedClass;

            var containingLine = _span.GetStartPoint(_snapshot).GetContainingLine();
            if (Step.GetStepImplementation(containingLine)!=null)
            {
                return;
            }

            var targetClass = Project.FindOrCreateClass(selectedClass);

            if (targetClass==null)
            {
                //TODO: Display error to user?
                return;
            }

            var functionCount = Project.GetFunctionsForClass(targetClass).Count();

            var implementationFunction = targetClass.AddFunction(string.Format("GaugeImpl{0}", functionCount+1), vsCMFunction.vsCMFunctionFunction, vsCMTypeRef.vsCMTypeRefVoid, -1,
                vsCMAccess.vsCMAccessPublic);

            if (Step.IsTable(containingLine))
            {
                implementationFunction.AddParameter(string.Format("table"), typeof(Table).Name);
            }
            else
            {
                var stepText = _span.GetText(_snapshot);
                var matches = Parser.StepRegex.Match(stepText);

                var paramCount = matches.Groups["stat"].Captures.Count + matches.Groups["dyn"].Captures.Count;

                for (var i = 1; i <= paramCount; i++)
                {
                    implementationFunction.AddParameter(string.Format("param{0}", i), vsCMTypeRef.vsCMTypeRefString);
                }
            }

            implementationFunction.AddAttribute("Step", Step.GetParsedStepValue(containingLine).ToLiteral(), -1);
            Project.NavigateToFunction(implementationFunction);

            _enabled = false;
        }

        public ReadOnlyCollection<SmartTagActionSet> ActionSets
        {
            get { return null; }
        }
        
        public ImageSource Icon { get; private set; }

        public string DisplayText
        {
            get { return _display; }
        }

        public bool IsEnabled
        {
            get { return _enabled; }
        }
    }
}