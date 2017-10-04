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
using Gauge.VisualStudio.Model;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Gauge.VisualStudio.Classification
{
    internal class Classifier : IClassifier
    {
        private static readonly Dictionary<Parser.TokenType, string> GaugeTypeMap =
            new Dictionary<Parser.TokenType, string>
            {
                {Parser.TokenType.Specification, "gauge.specification"},
                {Parser.TokenType.Scenario, "gauge.scenario"},
                {Parser.TokenType.Step, "gauge.step"},
                {Parser.TokenType.Comment, "gauge.comment"},
                {Parser.TokenType.Tag, "gauge.tag"},
                {Parser.TokenType.TagValue, "gauge.tagvalue"},
                {Parser.TokenType.StaticParameter, "gauge.static_param"},
                {Parser.TokenType.DynamicParameter, "gauge.dynamic_param"},
                {Parser.TokenType.TableParameter, "gauge.dynamic_param"},
                {Parser.TokenType.FileParameter, "gauge.dynamic_param"}
            };

        private readonly IClassificationTypeRegistryService _classificationRegistry;
        private readonly ITextBuffer _textBuffer;

        public Classifier(ITextBuffer textBuffer, IClassificationTypeRegistryService classificationRegistry)
        {
            _classificationRegistry = classificationRegistry;
            _textBuffer = textBuffer;

            _textBuffer.Changed += TextBufferChanged;
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var snapshot = span.Snapshot;

            var paragraph = GetEnclosingParagraph(span);

            var paragraphText = snapshot.GetText(paragraph);

            var spans = new List<ClassificationSpan>();

            foreach (var token in Parser.ParseMarkdownParagraph(paragraphText))
            {
                var type = GetClassificationTypeForMarkdownToken(token.TokenType);
                spans.Add(new ClassificationSpan(
                    new SnapshotSpan(paragraph.Start + token.Span.Start, token.Span.Length), type));
            }
            return spans;
        }

        private void TextBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            foreach (var change in e.Changes)
            {
                var paragraph = GetEnclosingParagraph(new SnapshotSpan(e.After, change.NewSpan));

                if (!Parser.ParagraphContainsMultilineTokens(paragraph.GetText())) continue;
                if (ClassificationChanged != null)
                    ClassificationChanged(this, new ClassificationChangedEventArgs(paragraph));
            }
        }

        private static SnapshotSpan GetEnclosingParagraph(SnapshotSpan span)
        {
            var snapshot = span.Snapshot;

            var startLine = span.Start.GetContainingLine();
            var startLineNumber = startLine.LineNumber;
            var endLineNumber = span.End <= startLine.EndIncludingLineBreak
                ? startLineNumber
                : snapshot.GetLineNumberFromPosition(span.End);

            var foundEmpty = false;
            while (startLineNumber > 0)
            {
                var lineEmpty = snapshot.GetLineFromLineNumber(startLineNumber).GetText().Trim().Length == 0;

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

        private IClassificationType GetClassificationTypeForMarkdownToken(Parser.TokenType tokenType)
        {
            string classificationType;
            if (!GaugeTypeMap.TryGetValue(tokenType, out classificationType))
                throw new ArgumentException(string.Format("Unable to find classification type for {0} tokenType",
                    tokenType));

            return _classificationRegistry.GetClassificationType(classificationType);
        }
    }
}