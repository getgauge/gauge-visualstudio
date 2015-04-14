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
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;

namespace Gauge.VisualStudio.Highlighting
{
    internal class StepAdornment : IDisposable
    {
        private readonly IWpfTextView _textView;
        private readonly ITagAggregator<UnimplementedStepTag> _createTagAggregator;
        private readonly IAdornmentLayer _adornmentLayer;
        private readonly Pen _pen = new Pen(Brushes.OrangeRed, 1.0) { DashStyle = DashStyles.Dash };
        private readonly DrawingBrush _drawingBrush = new DrawingBrush();

        public StepAdornment(IWpfTextView textView, ITagAggregator<UnimplementedStepTag> createTagAggregator)
        {
            _textView = textView;
            _createTagAggregator = createTagAggregator;
            _adornmentLayer = _textView.GetAdornmentLayer("StepArdornment");
            _textView.LayoutChanged += OnLayoutChanged;
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
            GaugeDTEProvider.DTE.ActiveDocument.Save();
            foreach (var line in e.NewOrReformattedLines)
            {
                CreateVisuals(line);
            }
        }

        public static bool TryGetText(IWpfTextView textView, ITextViewLine textViewLine, out string text)
        {
            var extent = textViewLine.Extent;
            var bufferGraph = textView.BufferGraph;
            try
            {
                var collection = bufferGraph.MapDownToSnapshot(extent, SpanTrackingMode.EdgeInclusive, textView.TextSnapshot);
                var span = new SnapshotSpan(collection[0].Start, collection[collection.Count - 1].End);
                text = span.GetText();
                return true;
            }
            catch
            {
                text = null;
                return false;
            }
        }

        private void CreateVisuals(ITextViewLine line)
        {
            var textViewLines = _textView.TextViewLines;
            string text;
            if (!TryGetText(_textView, line, out text)) return;

            foreach (var tag in _createTagAggregator.GetTags(line.Extent))
            {
                foreach (var span in tag.Span.GetSpans(_textView.TextSnapshot))
                {
                    SetBoundary(textViewLines, span, tag.Tag);
                }
            }
        }

        public void SetBoundary(IWpfTextViewLineCollection textViewLines, SnapshotSpan span, UnimplementedStepTag tag)
        {
            var g = textViewLines.GetMarkerGeometry(span);
            if (g == null) return;

            var drawing = new GeometryDrawing(_drawingBrush, _pen, g);
            drawing.Freeze();

            var drawingImage = new DrawingImage(drawing);
            drawingImage.Freeze();

            var image = new Image {Source = drawingImage};

            Canvas.SetLeft(image, g.Bounds.Left);
            Canvas.SetTop(image, g.Bounds.Top);

            _adornmentLayer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, tag, image, null);
        }

        public void Dispose()
        {
            _textView.LayoutChanged -= OnLayoutChanged;
        }
    }
}