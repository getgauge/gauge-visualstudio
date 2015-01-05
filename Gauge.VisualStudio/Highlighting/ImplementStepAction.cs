using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using EnvDTE;
using Gauge.CSharp.Lib;
using Gauge.VisualStudio.Classification;
using Gauge.VisualStudio.Models;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

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
            var containingLine = _span.GetStartPoint(_snapshot).GetContainingLine();
            if (Step.GetStepImplementation(containingLine)!=null)
            {
                return;
            }

            var targetClass = GaugeProject.GetAllClasses().First(element => element.Name == "StepImplementation") as CodeClass;

            var functionCount = GaugeProject.GetFunctionsForClass(targetClass).Count();

            if (targetClass==null)
            {
                return;
            }

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
            GaugeVSHelper.NavigateToFunction(implementationFunction);

            _enabled = false;
//            var filePicker = new FilePicker();
//            filePicker.ShowModal();
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