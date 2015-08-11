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
using Gauge.VisualStudio.UI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;

namespace Gauge.VisualStudio.Refactor
{
    public class RenameCommandFilter : IOleCommandTarget
    {
        private readonly IWpfTextView _view;

        public RenameCommandFilter(IWpfTextView view)
        {
            _view = view;
        }

        public IOleCommandTarget Next { get; set; }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if ((VSConstants.VSStd2KCmdID) prgCmds[0].cmdID != VSConstants.VSStd2KCmdID.RENAME)
                return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);

            prgCmds[0].cmdf = (uint) OLECMDF.OLECMDF_ENABLED | (uint) OLECMDF.OLECMDF_SUPPORTED;
            return VSConstants.S_OK;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            var hresult = VSConstants.S_OK;
            switch ((VSConstants.VSStd2KCmdID)nCmdID)
            {
                case VSConstants.VSStd2KCmdID.RENAME:
                    var caretBufferPosition = _view.Caret.Position.BufferPosition;
                    var originalText = caretBufferPosition.GetContainingLine().GetText();
                    if (!Parser.StepRegex.IsMatch(originalText))
                        return hresult;

                    var refactorDialog = new RefactorDialog(originalText);
                    var showModal = refactorDialog.ShowModal();
                    if (!showModal.HasValue || !showModal.Value)
                    {
                        return hresult;
                    }

                    var stepText = refactorDialog.StepText;

                    // need to update Gauge.CSharp.Lib
                    // requires refactoring of Lib

                    return hresult;
                default:
                    hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                    break;
            }
            return hresult;
        }
    }
}
