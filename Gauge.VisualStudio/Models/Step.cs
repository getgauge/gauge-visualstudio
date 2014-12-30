using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using EnvDTE;
using main;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Formatting;

namespace Gauge.VisualStudio.Models
{
    public class Step
    {
        public static IEnumerable<string> GetAll()
        {
            var gaugeApiConnection = GaugeDTEProvider.GetApiConnectionForActiveDocument();
            var stepsRequest = GetAllStepsRequest.DefaultInstance;
            var apiMessage = APIMessage.CreateBuilder()
                .SetMessageId(GenerateMessageId())
                .SetMessageType(APIMessage.Types.APIMessageType.GetAllStepsRequest)
                .SetAllStepsRequest(stepsRequest)
                .Build();

            var bytes = gaugeApiConnection.WriteAndReadApiMessage(apiMessage);

            return bytes.AllStepsResponse.AllStepsList.Select(x => x.ParameterizedStepValue);
        }

        public static CodeFunction GetStepImplementation(ITextSnapshotLine line, Project containingProject = null)
        {
            if (containingProject==null)
            {
                containingProject = GaugeDTEProvider.DTE.ActiveDocument.ProjectItem.ContainingProject;
            }

            var lineText = GetStepText(line);

            var stepRegex = new Regex(@"""([^""]*)""|\<([^\>]*)\>", RegexOptions.Compiled);
            var allClasses = GetCodeElementsFor(containingProject.CodeModel.CodeElements,
                vsCMElement.vsCMElementClass);

            foreach (var codeElement in allClasses)
            {
                if (!(codeElement is CodeClass)) continue;
                var codeClass = (CodeClass) codeElement;
                // get all methods implemented by this class
                var allFunctions = GetCodeElementsFor(codeClass.Members, vsCMElement.vsCMElementFunction);
                foreach (var codeFunction in allFunctions)
                {
                    var function = codeFunction as CodeFunction;
                    if (function == null) continue;
                    var allAttributes = GetCodeElementsFor(function.Attributes,
                        vsCMElement.vsCMElementAttribute);
                    foreach (dynamic attribute in allAttributes)
                    {
                        if (attribute.FullName != "Gauge.CSharp.Lib.Attribute.Step") continue;

                        foreach (var arg in attribute.Arguments)
                        {
                            string input = arg.Value.ToString().Trim('"');

                            if (stepRegex.Replace(input, "{}") != stepRegex.Replace(lineText, "{}"))
                                continue;

                            return function;
                        }
                    }
                }
            }
            return null;
        }

        public static string GetStepText(ITextSnapshotLine line)
        {
            var tableRegex = new Regex(@"[ ]*\|[\w ]+\|", RegexOptions.Compiled);
            var originalText = line.GetText();
            var lineText = originalText.Replace('*', ' ').Trim();
            var nextLineText = NextLineText(line);

            //if next line is a table then change the last word of the step to take in a special param
            if (tableRegex.IsMatch(nextLineText))
                lineText = string.Format("{0} {{}}", lineText);
            return lineText;
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
    }
}