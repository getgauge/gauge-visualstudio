using System.Collections.ObjectModel;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Gauge.VisualStudio.Highlighting
{
    internal class ImplementStepAction : ISmartTagAction
    {
        private readonly ITrackingSpan _span;
        private readonly ITextSnapshot _snapshot;
        private string _upper;
        private readonly string _display;
        private bool _enabled = true;

        public ImplementStepAction(ITrackingSpan trackingSpan, UnimplementedStepTagger unimplementedStepTagger)
        {
            _span = trackingSpan;
            _snapshot = _span.TextBuffer.CurrentSnapshot;
            _upper = _span.GetText(_snapshot).ToUpper();
            _display = "Implement Step";
        }

        public void Invoke()
        {
            _enabled = false;
        }

        public ReadOnlyCollection<SmartTagActionSet> ActionSets
        {
            get { return null; }
        }

        public ImageSource Icon { get; private set; }

        public string DisplayText
        {
            get { return _display; }
        }

        public bool IsEnabled
        {
            get { return _enabled; }
        }
    }
}