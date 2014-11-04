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
        [Export(typeof (IVsTextViewCreationListener))]
        [ContentType(GaugeContentTypeDefinitions.GaugeContentType)]
        [TextViewRole(PredefinedTextViewRoles.Document)]
        internal sealed class VsTextViewCreationListener : IVsTextViewCreationListener
        {
            [Import] private IVsEditorAdaptersFactoryService _adaptersFactory;

            [Import] private ICompletionBroker _completionBroker;

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
