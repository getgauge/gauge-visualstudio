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
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EnvDTE;
using Gauge.CSharp.Lib;
using Gauge.VisualStudio.Classification;
using Gauge.VisualStudio.Core.Extensions;
using Gauge.VisualStudio.Loggers;
using Gauge.VisualStudio.Model;
using Gauge.VisualStudio.UI;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Project = Gauge.VisualStudio.Model.Project;

namespace Gauge.VisualStudio.Highlighting
{
    internal class ImplementStepAction : ISmartTagAction
    {
        private readonly SnapshotSpan _span;
        private readonly UnimplementedStepTagger _unimplementedStepTagger;
        private readonly ITextSnapshot _snapshot;
        private readonly string _display;
        private bool _enabled = true;
        private readonly ITrackingSpan _trackingSpan;
        private readonly Step _step;
        private readonly Project _project;

        public ImplementStepAction(SnapshotSpan span, UnimplementedStepTagger unimplementedStepTagger, Project project)
        {
            _trackingSpan = span.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);

            _span = span;
            _unimplementedStepTagger = unimplementedStepTagger;
            _snapshot = _trackingSpan.TextBuffer.CurrentSnapshot;
            _display = "Implement Step";
            Icon = new BitmapImage(new Uri("pack://application:,,,/Gauge.VisualStudio;component/assets/glyphs/step.png"));
            _project = project;
            _step = new Step(GaugePackage.ActiveProject);
        }

        public void Invoke()
        {
            var classPicker = new ClassPicker();
            var selectedClass = string.Empty;
            classPicker.ShowModal();
            selectedClass = classPicker.SelectedClass;

            var containingLine = _trackingSpan.GetStartPoint(_snapshot).GetContainingLine();
            if (_project.GetStepImplementation(containingLine)!=null || selectedClass == null)
            {
                return;
            }

            CodeClass targetClass;
            try
            {
                targetClass = Project.FindOrCreateClass(GaugePackage.ActiveProject, selectedClass);
            }
            catch (ArgumentException ex)
            {
                StatusBarLogger.Log(ex.Message);
                return;
            }

            if (targetClass==null)
            {
                //TODO: Display error to user?
                return;
            }

            var functionCount = Project.GetFunctionsForClass(targetClass).Count();
            CodeFunction implementationFunction = null;

            try
            {
                implementationFunction = targetClass.AddFunction(string.Format("GaugeImpl{0}", functionCount + 1),
                    vsCMFunction.vsCMFunctionFunction, vsCMTypeRef.vsCMTypeRefVoid, -1,
                    vsCMAccess.vsCMAccessPublic);

                if (Step.HasTable(containingLine))
                {
                    implementationFunction.AddParameter("table", typeof (Table).Name);
                }
                else
                {
                    var step = _step.Parse(GaugePackage.ActiveProject, containingLine);
                    foreach (var parameter in step.Parameters)
                    {
                        implementationFunction.AddParameter(parameter, vsCMTypeRef.vsCMTypeRefString);
                    }
                }

                var codeAttribute = implementationFunction.AddAttribute("Step",
                    _step.GetParameterizedStepValue(GaugePackage.ActiveProject, containingLine).ToLiteral(), -1);

                if (codeAttribute == null)
                {
                    throw new ChangeRejectedException("Step Attribute not created");
                }

                targetClass.ProjectItem.Save();
                Project.RefreshImplementations(targetClass.ProjectItem);
                Project.NavigateToFunction(implementationFunction);
                _unimplementedStepTagger.MarkTagImplemented(_span);
                _enabled = false;
            }
            catch
            {
                if (implementationFunction != null)
                    targetClass.RemoveMember(implementationFunction);
            }
            finally
            {
                targetClass.ProjectItem.Save();
            }
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