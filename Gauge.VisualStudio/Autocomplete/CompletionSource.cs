using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Gauge.VisualStudio.AutoComplete
{
    public class CompletionSource
    {
        [Export(typeof(ICompletionSourceProvider))]
        [ContentType(GaugeContentTypeDefinitions.GaugeContentType)]
        [Name("gaugeCompletion")]
        class GaugeCompletionSourceProvider : ICompletionSourceProvider
        {
            public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
            {
                return new GaugeCompletionSource(textBuffer);
            }
        }

        class GaugeCompletionSource : ICompletionSource
        {
            private readonly ITextBuffer _buffer;
            private bool _disposed;
            
            public GaugeCompletionSource(ITextBuffer buffer)
            {
                _buffer = buffer;
            }

            public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
            {
                if (_disposed)
                    throw new ObjectDisposedException("GaugeCompletionSource");

                var completions =new List<Completion>(Steps.GetAll().Select(x => new Completion(string.Format("* {0}", x))));

                var snapshot = _buffer.CurrentSnapshot;
                var snapshotPoint = session.GetTriggerPoint(snapshot);
                if (snapshotPoint == null) return;
                
                var triggerPoint = (SnapshotPoint)snapshotPoint;

                var line = triggerPoint.GetContainingLine();
                var start = triggerPoint;

                while (start > line.Start && !char.IsWhiteSpace((start - 1).GetChar()))
                {
                    start -= 1;
                }

                var applicableTo = snapshot.CreateTrackingSpan(new SnapshotSpan(start, triggerPoint), SpanTrackingMode.EdgeInclusive);

                completionSets.Add(new CompletionSet("Gauge", "Gauge", applicableTo, completions, null));
            }

            public void Dispose()
            {
                _disposed = true;
            }
        }
         
    }
}