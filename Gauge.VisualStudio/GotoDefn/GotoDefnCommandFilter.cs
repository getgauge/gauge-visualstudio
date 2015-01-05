using System;
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
            switch ((VSConstants.VSStd97CmdID) nCmdID)
            {
                case VSConstants.VSStd97CmdID.GotoDefn:
                    var caretBufferPosition = TextView.Caret.Position.BufferPosition;
                    var originalText = caretBufferPosition.GetContainingLine().GetText();
                    if (!Parser.StepRegex.IsMatch(originalText))
                        return hresult;

                    var lineText = Step.GetStepText(caretBufferPosition.GetContainingLine());

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

                    var function = Step.GetStepImplementation(caretBufferPosition.GetContainingLine(), containingProject);

                    if (function==null)
                    {
                        return hresult;
                    }

                    GaugeVSHelper.NavigateToFunction(function);
                    return hresult;
                default:
                    hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                    break;
            }

            return hresult;
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