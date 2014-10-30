using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Gauge.VisualStudio.Classification
{
    class Classifier : IClassifier
    {
        readonly IClassificationTypeRegistryService _classificationRegistry;
        ITextBuffer _textBuffer;

        public Classifier(ITextBuffer textBuffer, IClassificationTypeRegistryService classificationRegistry)
        {
            _classificationRegistry = classificationRegistry;
            _textBuffer = textBuffer;

            _textBuffer.Changed += TextBufferChanged;
        }

        void TextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            foreach (var change in e.Changes)
            {
                var paragraph = GetEnclosingParagraph(new SnapshotSpan(e.After, change.NewSpan));

                if (!Parser.ParagraphContainsMultilineTokens(paragraph.GetText())) continue;
                if (ClassificationChanged != null)
                    ClassificationChanged(this, new ClassificationChangedEventArgs(paragraph));
            }
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        static SnapshotSpan GetEnclosingParagraph(SnapshotSpan span)
        {
            var snapshot = span.Snapshot;

            var startLine = span.Start.GetContainingLine();
            var startLineNumber = startLine.LineNumber;
            var endLineNumber = (span.End <= startLine.EndIncludingLineBreak) ? startLineNumber : snapshot.GetLineNumberFromPosition(span.End);

            var foundEmpty = false;
            while (startLineNumber > 0)
            {
                bool lineEmpty = snapshot.GetLineFromLineNumber(startLineNumber).GetText().Trim().Length == 0;

                if (lineEmpty)
                {
                    foundEmpty = true;
                }
                else if (foundEmpty)
                {
                    startLineNumber++;
                    break;
                }

                startLineNumber--;
            }

            foundEmpty = false;
            while (endLineNumber < snapshot.LineCount - 1)
            {
                var lineEmpty = snapshot.GetLineFromLineNumber(endLineNumber).GetText().Trim().Length == 0;

                if (lineEmpty)
                {
                    foundEmpty = true;
                }
                else if (foundEmpty)
                {
                    endLineNumber--;
                    break;
                }

                endLineNumber++;
            }

            var startPoint = snapshot.GetLineFromLineNumber(startLineNumber).Start;
            var endPoint = snapshot.GetLineFromLineNumber(endLineNumber).End;

            return new SnapshotSpan(startPoint, endPoint);
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var snapshot = span.Snapshot;

            var paragraph = GetEnclosingParagraph(span);

            var paragraphText = snapshot.GetText(paragraph);

            var spans = new List<ClassificationSpan>();

            foreach (var token in Parser.ParseMarkdownParagraph(paragraphText))
            {
                var type = GetClassificationTypeForMarkdownToken(token.TokenType);
                spans.Add(new ClassificationSpan(new SnapshotSpan(paragraph.Start + token.Span.Start, token.Span.Length), type));
            }
            return spans;
        }

        static readonly Dictionary<Parser.TokenType, string> GaugeTypeMap = new Dictionary<Parser.TokenType, string>
        {
            { Parser.TokenType.Specification, "gauge.specification" },
            { Parser.TokenType.Scenario, "gauge.scenario" },
            { Parser.TokenType.Step, "gauge.step" },
            { Parser.TokenType.Comment, "gauge.comment" },
        };

        IClassificationType GetClassificationTypeForMarkdownToken(Parser.TokenType tokenType)
        {
            string classificationType;
            if (!GaugeTypeMap.TryGetValue(tokenType, out classificationType))
                throw new ArgumentException(string.Format("Unable to find classification type for {0} tokenType", tokenType));

            return _classificationRegistry.GetClassificationType(classificationType);
        }
    }
}