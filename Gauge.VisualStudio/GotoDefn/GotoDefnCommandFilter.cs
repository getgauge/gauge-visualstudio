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
using Gauge.VisualStudio.Classification;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Project = Gauge.VisualStudio.Models.Project;

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
            switch ((VSConstants.VSStd97CmdID)nCmdID)
            {
                case VSConstants.VSStd97CmdID.GotoDefn:
                    var caretBufferPosition = TextView.Caret.Position.BufferPosition;
                    var originalText = caretBufferPosition.GetContainingLine().GetText();
                    if (!Parser.StepRegex.IsMatch(originalText))
                        return hresult;

                    //if the current step is a concept, then open the concept file.
                    //Gauge parses and caches the concepts, its location (file + line number).
                    //The plugin's job is to simply make an api call and fetch this information.

                    var stepImplementation = new Project().GetStepImplementation(caretBufferPosition.GetContainingLine());

                    if (stepImplementation != null)
                    {
                        stepImplementation.NavigateToImplementation();
                    }
                    return hresult;
                default:
                    hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                    break;
            }

            return hresult;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if ((VSConstants.VSStd97CmdID)prgCmds[0].cmdID != VSConstants.VSStd97CmdID.GotoDefn)
                return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);

            prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
            return VSConstants.S_OK;
        }
    }
}