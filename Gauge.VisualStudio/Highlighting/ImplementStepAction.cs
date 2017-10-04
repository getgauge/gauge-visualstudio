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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EnvDTE;
using Gauge.VisualStudio.Model;
using Gauge.VisualStudio.Model.Extensions;
using Gauge.VisualStudio.UI;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Project = Gauge.VisualStudio.Model.Project;

namespace Gauge.VisualStudio.Highlighting
{
    internal class ImplementStepAction : ISmartTagAction
    {
        private readonly IProject _project;
        private readonly ITextSnapshot _snapshot;
        private readonly SnapshotSpan _span;
        private readonly IStep _step;
        private readonly StepImplementationGenerator _stepImplementationGenerator;
        private readonly ITrackingSpan _trackingSpan;
        private readonly UnimplementedStepTagger _unimplementedStepTagger;

        public ImplementStepAction(SnapshotSpan span, UnimplementedStepTagger unimplementedStepTagger, IProject project)
        {
            _trackingSpan = span.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);

            _span = span;
            _unimplementedStepTagger = unimplementedStepTagger;
            _snapshot = _trackingSpan.TextBuffer.CurrentSnapshot;
            DisplayText = "Implement Step";
            Icon = new BitmapImage(
                new Uri("pack://application:,,,/Gauge.VisualStudio;component/assets/glyphs/step.png"));
            _project = project;
            var dteProject = span.Snapshot.GetProject(GaugePackage.DTE);
            var step = new Step(dteProject, span.Start.GetContainingLine());
            _stepImplementationGenerator = new StepImplementationGenerator(dteProject, project, step);
        }

        public void Invoke()
        {
            var classPicker = new ClassPicker(_snapshot.GetProject(GaugePackage.DTE));
            var selectedClass = string.Empty;
            classPicker.ShowModal();
            selectedClass = classPicker.SelectedClass;

            var containingLine = _trackingSpan.GetStartPoint(_snapshot).GetContainingLine();
            if (_project.GetStepImplementation(containingLine) != null || selectedClass == null)
                return;

            CodeClass targetClass;
            CodeFunction implementationFunction;
            var gotImplementation = _stepImplementationGenerator.TryGenerateMethodStub(selectedClass, containingLine,
                out targetClass,
                out implementationFunction);

            if (!gotImplementation) return;

            _project.RefreshImplementations(targetClass.ProjectItem);
            Project.NavigateToFunction(implementationFunction);
            _unimplementedStepTagger.MarkTagImplemented(_span);
            IsEnabled = false;
        }

        public ReadOnlyCollection<SmartTagActionSet> ActionSets => null;

        public ImageSource Icon { get; }

        public string DisplayText { get; }

        public bool IsEnabled { get; private set; } = true;
    }
}