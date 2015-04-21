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

using System.ComponentModel.Composition;
using Gauge.VisualStudio.AutoComplete;
using Gauge.VisualStudio.GotoDefn;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace Gauge.VisualStudio
{
    public class CommandController
    {
        [Export(typeof(IVsTextViewCreationListener))]
        [ContentType(GaugeContentTypeDefinitions.GaugeContentType)]
        [TextViewRole(PredefinedTextViewRoles.Document)]
        internal sealed class VsTextViewCreationListener : IVsTextViewCreationListener
        {
            [Import]
            private IVsEditorAdaptersFactoryService _adaptersFactory;

            [Import]
            private ICompletionBroker _completionBroker;

            public void VsTextViewCreated(IVsTextView textViewAdapter)
            {
                var view = _adaptersFactory.GetWpfTextView(textViewAdapter);

                var autoCompleteCommandFilter = new AutoCompleteCommandFilter(view, _completionBroker);

                IOleCommandTarget next;
                textViewAdapter.AddCommandFilter(autoCompleteCommandFilter, out next);
                autoCompleteCommandFilter.Next = next;

                var gotoDefnCommandFilter = new GotoDefnCommandFilter(view);
                textViewAdapter.AddCommandFilter(gotoDefnCommandFilter, out next);
                gotoDefnCommandFilter.Next = next;
            }
        }
    }
}
