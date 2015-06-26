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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Gauge.VisualStudio.Classification;
using Gauge.VisualStudio.Models;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Project = Gauge.VisualStudio.Models.Project;

namespace Gauge.VisualStudio.Highlighting
{
    internal class UnimplementedStepTagger : ITagger<UnimplementedStepTag>, IDisposable
    {
        private readonly ITextView _textView;
        private static readonly Project _project = new Project();

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
            if (TagsChanged == null || e.OldSnapshot == e.NewSnapshot) return;

            foreach (var span in e.NewOrReformattedSpans)
            {
                TagsChanged(this, new SnapshotSpanEventArgs(span));
            }
        }

        internal void MarkTagImplemented(SnapshotSpan span)
        {
            TagsChanged(this, new SnapshotSpanEventArgs(span));
        }

        public IEnumerable<ITagSpan<UnimplementedStepTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            foreach (var span in spans)
            {
                var line = span.Start.GetContainingLine();
                var text = line.GetText();
                var match = Parser.StepRegex.Match(text);
                var point = span.Start.Add(match.Index);
                var unimplementedStepSpan = new SnapshotSpan(span.Snapshot, new Span(point.Position, match.Length));
                if (!match.Success || _project.GetStepImplementation(line) != null)
                    continue;

                var actions = GetSmartTagActions(unimplementedStepSpan);
                var unimplementedStepTag = new UnimplementedStepTag(SmartTagType.Ephemeral, actions);
                yield return new TagSpan<UnimplementedStepTag>(unimplementedStepSpan, unimplementedStepTag);
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        private ReadOnlyCollection<SmartTagActionSet> GetSmartTagActions(SnapshotSpan span)
        {
            var actionList = new ReadOnlyCollection<ISmartTagAction>(new ISmartTagAction[] {new ImplementStepAction(span, this, _project)});
            return new ReadOnlyCollection<SmartTagActionSet>(new[] {new SmartTagActionSet(actionList)});
        }

        public void Dispose()
        {
            _textView.Caret.PositionChanged -= OnCaretMove;
            _textView.LayoutChanged -= OnLayoutChanged;
        }

        public void RaiseLayoutChanged()
        {
            var length = _textView.TextSnapshot.Length;
            TagsChanged(this, new SnapshotSpanEventArgs(new SnapshotSpan(new SnapshotPoint(_textView.TextSnapshot, 0), length)));
        }
    }
}