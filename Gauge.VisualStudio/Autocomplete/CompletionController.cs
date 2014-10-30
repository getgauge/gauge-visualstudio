using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Gauge.VisualStudio.AutoComplete
{
    public class CompletionController
    {
        [Export(typeof(IVsTextViewCreationListener))]
        [ContentType(GaugeContentTypeDefinitions.GaugeContentType)]
        [TextViewRole(PredefinedTextViewRoles.Interactive)]
        internal sealed class VsTextViewCreationListener : IVsTextViewCreationListener
        {
            [Import]
            IVsEditorAdaptersFactoryService _adaptersFactory;

            [Import]
            ICompletionBroker _completionBroker;

            public void VsTextViewCreated(IVsTextView textViewAdapter)
            {
                var view = _adaptersFactory.GetWpfTextView(textViewAdapter);

                var filter = new CommandFilter(view, _completionBroker);

                IOleCommandTarget next;
                textViewAdapter.AddCommandFilter(filter, out next);
                filter.Next = next;
            }
        }

        internal sealed class CommandFilter : IOleCommandTarget
        {
            ICompletionSession _currentSession;

            public CommandFilter(IWpfTextView textView, ICompletionBroker broker)
            {
                _currentSession = null;

                TextView = textView;
                Broker = broker;
            }

            public IWpfTextView TextView { get; private set; }
            public ICompletionBroker Broker { get; private set; }
            public IOleCommandTarget Next { get; set; }

            public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
            {
                var handled = false;
                var hresult = VSConstants.S_OK;

                // 1. Pre-process
                if (pguidCmdGroup == VSConstants.VSStd2K)
                {
                    switch ((VSConstants.VSStd2KCmdID)nCmdID)
                    {
                        case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                        case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                            handled = StartSession();
                            break;
                        case VSConstants.VSStd2KCmdID.TYPECHAR:
                            var lineText = TextView.Caret.Position.BufferPosition.GetContainingLine().GetText().Trim();
                            var stepRegex = new Regex(@"^(\*\s?\w*)$", RegexOptions.Compiled);
                            //the current character isn't yet available in the buffer!, add it to the text to check
                            if (stepRegex.IsMatch(string.Format("{0}{1}",lineText, GetTypeChar(pvaIn))))
                            {
                                StartSession();
                            }
                            Filter();                                
                            break;
                        case VSConstants.VSStd2KCmdID.RETURN:
                            handled = Complete(false);
                            break;
                        case VSConstants.VSStd2KCmdID.TAB:
                            handled = Complete(true);
                            break;
                        case VSConstants.VSStd2KCmdID.CANCEL:
                            handled = Cancel();
                            break;
                    }
                    if ((VSConstants.VSStd97CmdID) nCmdID == VSConstants.VSStd97CmdID.GotoDefn)
                    {
                        var lineText = TextView.Caret.Position.BufferPosition.GetContainingLine().GetText().Trim();
                        var codeElements = GaugeDTEProvider.DTE.Solution.Projects.Item(1).CodeModel.CodeElements;
                        var elements = new List<CodeElement>();
                        for (var i = 0; i < codeElements.Count; i++)
                        {
                            if(codeElements.Item(i).Kind==vsCMElement.vsCMElementFunction)
                                elements.Add(codeElements.Item(i));
                        }
                    }
                }

                if (!handled)
                    hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

                return hresult;
            }

            private static char GetTypeChar(IntPtr pvaIn)
            {
                return (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
            }

            private void Filter()
            {
                if (_currentSession == null)
                    return;

                _currentSession.SelectedCompletionSet.SelectBestMatch();
                _currentSession.SelectedCompletionSet.Recalculate();
            }

            bool Cancel()
            {
                if (_currentSession == null)
                    return false;

                _currentSession.Dismiss();

                return true;
            }

            bool Complete(bool force)
            {
                if (_currentSession == null)
                    return false;

                if (!_currentSession.SelectedCompletionSet.SelectionStatus.IsSelected && !force)
                {
                    _currentSession.Dismiss();
                    return false;
                }
                _currentSession.Commit();
                return true;
            }

            bool StartSession()
            {
                if (_currentSession != null)
                    return false;

                var caret = TextView.Caret.Position.BufferPosition;
                var snapshot = caret.Snapshot;

                _currentSession = !Broker.IsCompletionActive(TextView) ? Broker.CreateCompletionSession(TextView, snapshot.CreateTrackingPoint(caret, PointTrackingMode.Positive), true) : Broker.GetSessions(TextView)[0];
                _currentSession.Dismissed += (sender, args) => _currentSession = null;

                if (!_currentSession.IsStarted)
                {
                    _currentSession.Start();
                }

                return true;
            }

            public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
            {
                if (pguidCmdGroup != VSConstants.VSStd2K)
                    return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
                switch ((VSConstants.VSStd2KCmdID)prgCmds[0].cmdID)
                {
                    case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                        prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
                        return VSConstants.S_OK;
                }
                return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
            }
        }
         
    }
}