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
using System.Linq;
using Gauge.Messages;
using Gauge.VisualStudio.Core;
using Gauge.VisualStudio.Model;
using Gauge.VisualStudio.Model.Extensions;
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
            if ((VSConstants.VSStd2KCmdID) nCmdID == VSConstants.VSStd2KCmdID.RENAME)
            {
                GaugePackage.DTE.ExecuteCommand("File.SaveAll");
                var caretBufferPosition = _view.Caret.Position.BufferPosition;
                var currentLine = caretBufferPosition.GetContainingLine();
                var lineText = currentLine.GetText();
                if (!Parser.StepRegex.IsMatch(lineText))
                    return hresult;

                var originalText = Step.GetStepText(currentLine);

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
                if (startWaitDialog != VSConstants.S_OK)
                {
                    return hresult;
                }

                var undoContext = GaugePackage.DTE.UndoContext;
                undoContext.Open("GaugeRefactoring");
                try
                {
                    var project = _view.TextBuffer.CurrentSnapshot.GetProject(GaugePackage.DTE);
                    var response = RefactorUsingGaugeDaemon(newText, originalText, project);

                    if (!response.PerformRefactoringResponse.Success)
                    {
                        var errorMessage = string.Empty;
                        if (response.HasError)
                        {
                            errorMessage = string.Format("{0}\n", response.Error.Error);
                        }
                        if (response.HasPerformRefactoringResponse &&
                            response.PerformRefactoringResponse.ErrorsCount > 0)
                        {
                            response.PerformRefactoringResponse.ErrorsList.Aggregate(errorMessage,
                                (s, s1) => string.Format("{0}\n{1}", s, s1));
                        }
                        GaugeService.DisplayGaugeNotStartedMessage(
                            "Refactoring failed.\nCheck Gauge output pane for details.",
                            string.Format("Failed to refactor {0} to {1}. Error:\n{2}", originalText, newText,
                                errorMessage));
                        return VSConstants.S_FALSE;
                    }

                    ReloadChangedDocuments(response);
                    new Project(GaugePackage.DTE).RefreshImplementationsForActiveProject();
                }
                finally
                {
                    int cancelled;
                    progressDialog.EndWaitDialog(out cancelled);
                    undoContext.Close();
                }
                return hresult;
            }
            hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            return hresult;
        }

        private static APIMessage RefactorUsingGaugeDaemon(string newText, string originalText, EnvDTE.Project project)
        {
            var performRefactoringRequest = PerformRefactoringRequest
                .CreateBuilder()
                .SetNewStep(newText)
                .SetOldStep(originalText)
                .Build();
            var apiConnection = GaugeService.GetApiConnectionFor(project);
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
