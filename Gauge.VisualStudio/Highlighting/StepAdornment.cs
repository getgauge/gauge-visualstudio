using System.Windows.Controls;
using System.Windows.Media;
using EnvDTE;
using Gauge.VisualStudio.Classification;
using Gauge.VisualStudio.Models;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;

namespace Gauge.VisualStudio.Highlighting
{
    internal class StepAdornment
    {
        private readonly IWpfTextView _textView;
        private readonly IAdornmentLayer _adornmentLayer;
        private readonly Pen _pen = new Pen(Brushes.OrangeRed, 1.0) { DashStyle = DashStyles.Dash };
        private readonly DrawingBrush _drawingBrush = new DrawingBrush();

        public StepAdornment(IWpfTextView textView)
        {
            _textView = textView;
            _adornmentLayer = _textView.GetAdornmentLayer("StepArdornment");
            _textView.LayoutChanged += OnLayoutChanged;
        }

        private void OnLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
        {
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
                var collection = bufferGraph.MapDownToSnapshot(extent, SpanTrackingMode.EdgeInclusive,
                    textView.TextSnapshot);
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

            var match = Parser.StepRegex.Match(text);
            if (!match.Success || GetStepImplementation(line) != null || Concept.Search(text) != null)
                return;

            var matchStart = line.Start.Position;
            var span = new SnapshotSpan(_textView.TextSnapshot, Span.FromBounds(matchStart, matchStart + match.Length));
            SetBoundary(textViewLines, span);
        }

        private static CodeFunction GetStepImplementation(ITextViewLine line)
        {
            var snapshotLine = line.Snapshot.GetLineFromPosition(line.Start.Position);
            return Step.GetStepImplementation(snapshotLine);
        }

        public void SetBoundary(IWpfTextViewLineCollection textViewLines, SnapshotSpan span)
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

            _adornmentLayer.AddAdornment(AdornmentPositioningBehavior.TextRelative, span, null, image, null);
        }
    }
}