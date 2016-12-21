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
using System.Runtime.InteropServices;
using Gauge.VisualStudio.Model;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Gauge.VisualStudio.AutoComplete
{
    internal sealed class AutoCompleteCommandFilter : IOleCommandTarget
    {
        private readonly SVsServiceProvider _serviceProvider;
        private ICompletionSession _currentSession;

        public AutoCompleteCommandFilter(IWpfTextView textView, ICompletionBroker broker, SVsServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _currentSession = null;

            TextView = textView;
            Broker = broker;
        }

        public IWpfTextView TextView { get; private set; }
        public ICompletionBroker Broker { get; private set; }
        public IOleCommandTarget Next { get; set; }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (VsShellUtilities.IsInAutomationFunction(_serviceProvider))
            {
                return Next.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }
            var commandID = nCmdID;
            var typedChar = char.MinValue;
            if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
            {
                typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
            }


            if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN
                || nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
            {
                if (_currentSession != null && !_currentSession.IsDismissed)
                {
                    if (_currentSession.SelectedCompletionSet.SelectionStatus.IsSelected)
                    {
                        _currentSession.Commit();
                        return VSConstants.S_OK;
                    }
                    _currentSession.Dismiss();
                }
            }

            var retVal = Next.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            var handled = false;

            switch ((VSConstants.VSStd2KCmdID)commandID)
            {
                case VSConstants.VSStd2KCmdID.COMPLETEWORD:
                case VSConstants.VSStd2KCmdID.AUTOCOMPLETE:
                    if (_currentSession == null || _currentSession.IsDismissed)
                        TriggerCompletion();
                    Filter();
                    handled = true;
                    break;
                case VSConstants.VSStd2KCmdID.TYPECHAR:
                    if (!typedChar.Equals(char.MinValue) && char.IsLetterOrDigit(typedChar))
                    {
                        if (_currentSession == null || _currentSession.IsDismissed)
                            TriggerCompletion();
                        Filter();
                        handled = true;
                    }
                    break;
                case VSConstants.VSStd2KCmdID.BACKSPACE:
                case VSConstants.VSStd2KCmdID.DELETE:
                    TriggerCompletion(true);
                    Filter();
                    handled = true;
                    break;
            }
            return handled ? VSConstants.S_OK : retVal;
        }

        private void Filter()
        {
            if (_currentSession == null || _currentSession.IsDismissed) return;
            _currentSession.SelectedCompletionSet.SelectBestMatch();
            _currentSession.SelectedCompletionSet.Recalculate();
        }

        private void TriggerCompletion(bool force = false)
        {
            if (force && _currentSession != null && !_currentSession.IsDismissed)
            {
                _currentSession.Dismiss();
            }
            var lineText = TextView.Caret.Position.BufferPosition.GetContainingLine().GetText().Trim();
            if (!Parser.StepRegex.IsMatch(lineText)) return;

            var caretPoint = TextView.Caret.Position.Point.GetPoint(
                textBuffer => (!textBuffer.ContentType.IsOfType("projection")), 
                            PositionAffinity.Predecessor);
            if (!caretPoint.HasValue) return;

            _currentSession = Broker.CreateCompletionSession
                (TextView,
                    caretPoint.Value.Snapshot.CreateTrackingPoint(caretPoint.Value.Position, PointTrackingMode.Positive),
                    true);

            _currentSession.Dismissed += OnSessionDismissed;
            _currentSession.Start();
        }

        private void OnSessionDismissed(object sender, EventArgs e)
        {
             _currentSession.Dismissed -= OnSessionDismissed;
            _currentSession = null;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
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