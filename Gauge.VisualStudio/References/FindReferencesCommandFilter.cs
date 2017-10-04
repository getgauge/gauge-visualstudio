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
using EnvDTE;
using EnvDTE80;
using Gauge.VisualStudio.Model;
using Gauge.VisualStudio.Model.Extensions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;

namespace Gauge.VisualStudio.References
{
    internal sealed class FindReferencesCommandFilter : IOleCommandTarget
    {
        public FindReferencesCommandFilter(IWpfTextView textView)
        {
            TextView = textView;
        }

        public IOleCommandTarget Next { get; set; }
        private IWpfTextView TextView { get; }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var hresult = VSConstants.S_OK;
            switch ((VSConstants.VSStd97CmdID) nCmdID)
            {
                case VSConstants.VSStd97CmdID.FindReferences:
                    var caretBufferPosition = TextView.Caret.Position.BufferPosition;
                    var originalText = Step.GetStepText(caretBufferPosition.GetContainingLine());

                    var findRegex =
                        new GaugeServiceClient().GetFindRegex(caretBufferPosition.Snapshot.GetProject(GaugePackage.DTE),
                            originalText);

                    var _dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE2;
                    var find = (Find2) _dte.Find;

                    var types = find.FilesOfType;
                    var matchCase = find.MatchCase;
                    var matchWord = find.MatchWholeWord;

                    find.WaitForFindToComplete = false;
                    find.Action = vsFindAction.vsFindActionFindAll;
                    find.Backwards = false;
                    find.MatchInHiddenText = true;
                    find.MatchWholeWord = true;
                    find.MatchCase = false;
                    find.PatternSyntax = vsFindPatternSyntax.vsFindPatternSyntaxRegExpr;
                    find.ResultsLocation = vsFindResultsLocation.vsFindResults1;
                    find.SearchSubfolders = true;
                    find.Target = vsFindTarget.vsFindTargetSolution;
                    find.FindWhat = findRegex;
                    find.Execute();

                    find.FilesOfType = types;
                    find.MatchCase = matchCase;
                    find.MatchWholeWord = matchWord;

                    return hresult;
                default:
                    hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                    break;
            }
            return hresult;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if ((VSConstants.VSStd97CmdID) prgCmds[0].cmdID != VSConstants.VSStd97CmdID.FindReferences)
                return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);

            prgCmds[0].cmdf = (uint) OLECMDF.OLECMDF_ENABLED | (uint) OLECMDF.OLECMDF_SUPPORTED;
            return VSConstants.S_OK;
        }
    }
}