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
using Gauge.Messages;
using Gauge.VisualStudio.Classification;
using Gauge.VisualStudio.UI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

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
                    var currentLine = caretBufferPosition.GetContainingLine();
                    var lineText = currentLine.GetText();
                    if (!Parser.StepRegex.IsMatch(lineText))
                        return hresult;

                    var originalText = Models.Step.GetStepText(currentLine);

                    var refactorDialog = new RefactorDialog(originalText);
                    var showModal = refactorDialog.ShowModal();
                    if (!showModal.HasValue || !showModal.Value)
                    {
                        return hresult;
                    }

                    var newText = refactorDialog.StepText;
                    var progressDialog = CreateProgressDialog();
                    var startWaitDialog = progressDialog.StartWaitDialog("Gauge - Renaming",
                        string.Format("Original: {0}", originalText), string.Format("To: {0}", newText), null,
                        "Refactoring Step", 0, false, true);
                    if (startWaitDialog == VSConstants.S_OK)
                    {
                        var undoContext = GaugePackage.DTE.UndoContext;
                        undoContext.Open("GaugeRefactoring");
                        try
                        {
                            var response = RefactorUsingGaugeDaemon(newText, originalText);

                            if (!response.PerformRefactoringResponse.Success)
                                return VSConstants.S_FALSE;

                            ReloadChangedDocuments(response);
                            Models.Project.RefreshImplementationsForActiveProject();
                        }
                        finally
                        {
                            int cancelled;
                            progressDialog.EndWaitDialog(out cancelled);
                            undoContext.Close();
                        }
                    }
                    return hresult;
                default:
                    hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
                    break;
            }
            return hresult;
        }

        private static APIMessage RefactorUsingGaugeDaemon(string newText, string originalText)
        {
            var performRefactoringRequest = PerformRefactoringRequest
                .CreateBuilder()
                .SetNewStep(newText)
                .SetOldStep(originalText)
                .Build();
            var apiConnection = GaugeDaemonHelper.GetApiConnectionForActiveDocument();
            var apiMessage = APIMessage.CreateBuilder()
                .SetPerformRefactoringRequest(performRefactoringRequest)
                .SetMessageType(APIMessage.Types.APIMessageType.PerformRefactoringRequest)
                .SetMessageId(7)
                .Build();
            var response = apiConnection.WriteAndReadApiMessage(apiMessage);
            return response;
        }

        private static void ReloadChangedDocuments(APIMessage response)
        {
            var serviceProvider = Package.GetGlobalService(typeof(IServiceProvider)) as IServiceProvider;
            var runningDocumentTable = new RunningDocumentTable(new ServiceProvider(serviceProvider));

            foreach (var file in response.PerformRefactoringResponse.FilesChangedList)
            {
                var vsPersistDocData = runningDocumentTable.FindDocument(file) as IVsPersistDocData;
                if (vsPersistDocData != null)
                {
                    vsPersistDocData.ReloadDocData((uint) _VSRELOADDOCDATA.RDD_IgnoreNextFileChange);
                }
            }
        }

        private static IVsThreadedWaitDialog2 CreateProgressDialog()
        {
            IVsThreadedWaitDialog2 dialog;
            var oleServiceProvider = Package.GetGlobalService(typeof (IServiceProvider)) as IServiceProvider;
            var dialogFactory = new ServiceProvider(oleServiceProvider)
                .GetService(typeof (SVsThreadedWaitDialogFactory)) as IVsThreadedWaitDialogFactory;
            dialogFactory.CreateInstance(out dialog);
            return dialog;
        }
    }
}
