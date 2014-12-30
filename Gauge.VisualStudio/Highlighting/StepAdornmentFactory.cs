using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Gauge.VisualStudio.Highlighting
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(GaugeContentTypeDefinitions.GaugeContentType)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class StepAdornmentFactory : IWpfTextViewCreationListener
    {
        [Export(typeof(AdornmentLayerDefinition))]
        [Name("StepArdornment")]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
        [TextViewRole(PredefinedTextViewRoles.Interactive)]
        public AdornmentLayerDefinition GaugeAdornmentLayer = null;


        public void TextViewCreated(IWpfTextView textView)
        {
            new StepAdornment(textView);
        }
    }
}