using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Gauge.VisualStudio.Highlighting
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(GaugeContentTypeDefinitions.GaugeContentType)]
    [Order(Before = "default")]
    [TagType(typeof(UnimplementedStepTag))]
    public class StepTaggerProvider : IViewTaggerProvider
    {
        private readonly Dictionary<ITextView, UnimplementedStepTagger> _taggers = new Dictionary<ITextView, UnimplementedStepTagger>();

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (buffer == null || textView == null)
            {
                return null;
            }

            if (buffer != textView.TextBuffer) return null;
            
            if (!_taggers.ContainsKey(textView))
            {
                _taggers[textView] = new UnimplementedStepTagger(textView);
            }
            return _taggers[textView] as ITagger<T>;
        }
    }
}