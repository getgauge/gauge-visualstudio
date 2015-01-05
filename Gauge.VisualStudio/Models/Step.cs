using System;
using System.Collections;
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
        private static IEnumerable<GaugeImplementation> _gaugeImplementations;

        public static IEnumerable<string> GetAll()
        {
            return GetAllSteps().Select(x => x.ParameterizedStepValue);
        }

        public static string GetParsedStepValue(ITextSnapshotLine input)
        {
            var stepValueFromInput = GetStepValueFromInput(GetStepText(input));
            return GetAllSteps(true).First(value => value.StepValue == stepValueFromInput)
                   .ParameterizedStepValue;
        }

        public static CodeFunction GetStepImplementation(ITextSnapshotLine line, Project containingProject = null)
        {
            if (containingProject==null)
            {
                containingProject = GaugeDTEProvider.DTE.ActiveDocument.ProjectItem.ContainingProject;
            }

            var lineText = GetStepText(line);

            _gaugeImplementations = _gaugeImplementations ?? GetGaugeImplementations(containingProject);
            var gaugeImplementation = _gaugeImplementations.FirstOrDefault(implementation => implementation.ContainsFor(lineText));
            return gaugeImplementation == null ? null : gaugeImplementation.Function;
        }

        private static IEnumerable<GaugeImplementation> GetGaugeImplementations(Project containingProject = null)
        {
            var gaugeImplementations = new List<GaugeImplementation>();
            var allClasses = GetAllClasses(containingProject);

            foreach (var codeElement in allClasses)
            {
                if (!(codeElement is CodeClass)) continue;
                var codeClass = (CodeClass) codeElement;
                var allFunctions = GetCodeElementsFor(codeClass.Members, vsCMElement.vsCMElementFunction);
                foreach (var codeFunction in allFunctions)
                {
                    var function = codeFunction as CodeFunction;
                    if (function == null) continue;
                    var allAttributes = GetCodeElementsFor(function.Attributes, vsCMElement.vsCMElementAttribute);

                    var attribute = allAttributes.FirstOrDefault(a => a.FullName == "Gauge.CSharp.Lib.Attribute.Step");
                    if (attribute != null)
                        gaugeImplementations.Add(new GaugeImplementation {Function = function, StepAttribute = attribute});
                }
            }

            return gaugeImplementations;
        }
        
        public static IEnumerable<CodeElement> GetAllClasses(Project containingProject=null)
        {
            if (containingProject==null)
            {
                containingProject = GaugeDTEProvider.DTE.ActiveDocument.ProjectItem.ContainingProject;
            }
            return GetCodeElementsFor(containingProject.CodeModel.CodeElements, vsCMElement.vsCMElementClass);
        }

        public static string GetStepText(ITextSnapshotLine line)
        {
            var originalText = line.GetText();
            var tableRegex = new Regex(@"[ ]*\|[\w ]+\|", RegexOptions.Compiled);
            var lineText = originalText.Replace('*', ' ').Trim();
            var nextLineText = NextLineText(line);

            //if next line is a table then change the last word of the step to take in a special param
            if (tableRegex.IsMatch(nextLineText))
                lineText = string.Format("{0} {{}}", lineText);
            return lineText;
        }

        public static void Refresh()
        {
            _allSteps = GetAllStepsFromGauge();
            _gaugeImplementations =
                GetGaugeImplementations(GaugeDTEProvider.DTE.ActiveDocument.ProjectItem.ContainingProject);
        }

        internal class GaugeImplementation
        {
            public CodeFunction Function { get; set; }
            public dynamic StepAttribute { get; set; }

            public bool ContainsFor(string givenText)
            {
                foreach (var arg in StepAttribute.Arguments)
                {
                    string input = arg.Value.ToString().Trim('"');

                    if (GetStepValueFromInput(input) == GetStepValueFromInput(givenText))
                        return true;
                }
                return false;
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

        private static IEnumerable<CodeElement> GetCodeElementsFor(IEnumerable elements, vsCMElement type)
        {
            var codeElements = new List<CodeElement>();

            foreach (CodeElement elem in elements)
            {
                if (elem.Kind == vsCMElement.vsCMElementNamespace)
                {
                    codeElements.AddRange(GetCodeElementsFor(((CodeNamespace) elem).Members, type));
                }
                else if (elem.InfoLocation == vsCMInfoLocation.vsCMInfoLocationExternal)
                {
                    continue;
                }
                else if (elem.IsCodeType)
                {
                    codeElements.AddRange(GetCodeElementsFor(((CodeType) elem).Members, type));
                }
                if (elem.Kind == type)
                    codeElements.Add(elem);
            }
    
            return codeElements;
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

        private static string GetStepValueFromInput(string input)
        {
            var stepRegex = new Regex(@"""([^""]*)""|\<([^\>]*)\>", RegexOptions.Compiled);
            return stepRegex.Replace(input, "{}");
        }

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