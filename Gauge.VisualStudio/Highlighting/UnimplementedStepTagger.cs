using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EnvDTE;
using Gauge.VisualStudio.Classification;
using Gauge.VisualStudio.Models;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;

namespace Gauge.VisualStudio.Highlighting
{
    internal class UnimplementedStepTagger : ITagger<UnimplementedStepTag>, IDisposable
    {
        private readonly ITextView _textView;

        public UnimplementedStepTagger(ITextView textView)
        {
            _textView = textView;
            _textView.LayoutChanged += OnLayoutChanged;
            _textView.Caret.PositionChanged += OnCaretMove;
        }

        private void OnCaretMove(object sender, CaretPositionChangedEventArgs e)
        {
            if (TagsChanged == null) return;
            var line = _textView.GetTextViewLineContainingBufferPosition(e.NewPosition.BufferPosition);
            TagsChanged(this, new SnapshotSpanEventArgs(line.Extent));
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            if (TagsChanged == null) return;
            foreach (var span in e.NewOrReformattedSpans)
            {
                TagsChanged(this, new SnapshotSpanEventArgs(span));
            }
        }

        public IEnumerable<ITagSpan<UnimplementedStepTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (var span in spans)
            {
                var text = span.GetText();
                var match = Parser.StepRegex.Match(text);
                var point = span.Start.Add(match.Index);
                var unimplementedStepSpan = new SnapshotSpan(span.Snapshot, new Span(point.Position, match.Length));
                if (!match.Success || GetStepImplementation(unimplementedStepSpan) != null || Concept.Search(text) != null)
                    continue;

                var actions = GetSmartTagActions(unimplementedStepSpan);
                yield return new TagSpan<UnimplementedStepTag>(unimplementedStepSpan, new UnimplementedStepTag(SmartTagType.Ephemeral, actions));
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        private ReadOnlyCollection<SmartTagActionSet> GetSmartTagActions(SnapshotSpan span)
        {
            var trackingSpan = span.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive);
            var actionList = new ReadOnlyCollection<ISmartTagAction>(new ISmartTagAction[] {new ImplementStepAction(trackingSpan, this)});
            return new ReadOnlyCollection<SmartTagActionSet>(new[] {new SmartTagActionSet(actionList)});
        }

        private static CodeFunction GetStepImplementation(SnapshotSpan span)
        {
            var snapshotLine = span.Snapshot.GetLineFromPosition(span.Start.Position);
            return Step.GetStepImplementation(snapshotLine);
        }

        public void Dispose()
        {
            _textView.Caret.PositionChanged -= OnCaretMove;
            _textView.LayoutChanged -= OnLayoutChanged;
        }
    }
}