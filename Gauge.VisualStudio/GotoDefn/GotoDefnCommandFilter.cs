using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using EnvDTE;
using Gauge.VisualStudio.Classification;
using Gauge.VisualStudio.Models;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Gauge.VisualStudio.GotoDefn
{
    internal sealed class GotoDefnCommandFilter : IOleCommandTarget
    {
        public GotoDefnCommandFilter(IWpfTextView textView)
        {
            TextView = textView;
        }

        private IWpfTextView TextView { get; set; }
        public IOleCommandTarget Next { get; set; }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var hresult = VSConstants.S_OK;
            if ((VSConstants.VSStd97CmdID) nCmdID == VSConstants.VSStd97CmdID.GotoDefn)
            {
                var caretBufferPosition = TextView.Caret.Position.BufferPosition;
                var tableRegex = new Regex(@"[ ]*\|[\w ]+\|", RegexOptions.Compiled);
                var originalText = caretBufferPosition.GetContainingLine().GetText();
                if (!Parser.StepRegex.IsMatch(originalText))
                    return hresult;

                var lineText = originalText.Replace('*', ' ').Trim();
                var nextLineText = NextLineText(caretBufferPosition.GetContainingLine());

                //if next line is a table then change the last word of the step to take in a special param
                if (tableRegex.IsMatch(nextLineText))
                    lineText = string.Format("{0} {{}}", lineText);

                var stepRegex = new Regex(@"""([^""]*)""|\<([^\>]*)\>", RegexOptions.Compiled);
                var dte = GaugeDTEProvider.DTE;
                var containingProject = dte.ActiveDocument.ProjectItem.ContainingProject;
                
                //if the current step is a concept, then open the concept file.
                //Gauge parses and caches the concepts, its location (file + line number).
                //The plugin's job is to simply make an api call and fetch this information.

                var concept = Concept.Search(lineText);
                if (concept != null)
                {
                    var window = dte.ItemOperations.OpenFile(concept.FilePath);
                    window.Activate();

                    var textSelection = window.Selection as TextSelection;
                    if (textSelection != null) 
                        textSelection.MoveTo(concept.LineNumber, 0);
                    return hresult;
                }

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

                                if (!function.ProjectItem.IsOpen) function.ProjectItem.Open();

                                var startPoint = function.GetStartPoint(vsCMPart.vsCMPartHeader);
                                startPoint.TryToShow();
                                startPoint.Parent.Selection.MoveToPoint(startPoint);
                                return hresult;
                            }
                        }
                    }
                }
            }
            else 
                hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            return hresult;
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

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if ((VSConstants.VSStd97CmdID) prgCmds[0].cmdID != VSConstants.VSStd97CmdID.GotoDefn)
                return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);

            prgCmds[0].cmdf = (uint) OLECMDF.OLECMDF_ENABLED | (uint) OLECMDF.OLECMDF_SUPPORTED;
            return VSConstants.S_OK;
        }
    }
}