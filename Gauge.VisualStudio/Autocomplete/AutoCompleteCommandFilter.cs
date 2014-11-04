using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Gauge.VisualStudio.AutoComplete
{
    internal sealed class AutoCompleteCommandFilter : IOleCommandTarget
    {
        private ICompletionSession _currentSession;

        public AutoCompleteCommandFilter(IWpfTextView textView, ICompletionBroker broker)
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
                switch ((VSConstants.VSStd2KCmdID) nCmdID)
                {
                    case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                        handled = StartSession();
                        break;
                    case VSConstants.VSStd2KCmdID.TYPECHAR:
                        var lineText = TextView.Caret.Position.BufferPosition.GetContainingLine().GetText().Trim();
                        var stepRegex = new Regex(@"^(\*\s?\w*)$", RegexOptions.Compiled);
                        //the current character isn't yet available in the buffer!, add it to the text to check
                        if (stepRegex.IsMatch(string.Format("{0}{1}", lineText, GetTypeChar(pvaIn))))
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
            }

            if (!handled)
                hresult = Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            return hresult;
        }

        private static char GetTypeChar(IntPtr pvaIn)
        {
            return (char) (ushort) Marshal.GetObjectForNativeVariant(pvaIn);
        }

        private void Filter()
        {
            if (_currentSession == null)
                return;

            _currentSession.SelectedCompletionSet.SelectBestMatch();
            _currentSession.SelectedCompletionSet.Recalculate();
        }

        private bool Cancel()
        {
            if (_currentSession == null)
                return false;

            _currentSession.Dismiss();

            return true;
        }

        private bool Complete(bool force)
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

        private bool StartSession()
        {
            if (_currentSession != null)
                return false;

            var caret = TextView.Caret.Position.BufferPosition;
            var snapshot = caret.Snapshot;

            _currentSession = !Broker.IsCompletionActive(TextView)
                ? Broker.CreateCompletionSession(TextView,
                    snapshot.CreateTrackingPoint(caret, PointTrackingMode.Positive), true)
                : Broker.GetSessions(TextView)[0];
            _currentSession.Dismissed += (sender, args) => _currentSession = null;

            if (!_currentSession.IsStarted)
            {
                _currentSession.Start();
            }

            return true;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            try
            {
                switch ((VSConstants.VSStd2KCmdID) prgCmds[0].cmdID)
                {
                    case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                    case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                        prgCmds[0].cmdf = (uint) OLECMDF.OLECMDF_ENABLED | (uint) OLECMDF.OLECMDF_SUPPORTED;
                        return VSConstants.S_OK;
                }
            }
            catch
            {
                //do nothing
            }

            return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}