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
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media.Imaging;
using Gauge.VisualStudio.Models;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
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
            [Import]
            internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }
            public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
            {
                return new GaugeCompletionSource(this, textBuffer);
            }
        }

        class GaugeCompletionSource : ICompletionSource
        {
            private readonly GaugeCompletionSourceProvider _gaugeCompletionSourceProvider;
            private readonly ITextBuffer _buffer;
            private bool _disposed;
            private readonly Concept _concept = new Concept();
            private readonly Step _step = new Step();

            public GaugeCompletionSource(GaugeCompletionSourceProvider gaugeCompletionSourceProvider, ITextBuffer buffer)
            {
                _gaugeCompletionSourceProvider = gaugeCompletionSourceProvider;
                _buffer = buffer;
            }

            public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
            {
                if (_disposed)
                    throw new ObjectDisposedException("GaugeCompletionSource");

                BitmapSource stepImageSource = new BitmapImage(new Uri("pack://application:,,,/Gauge.VisualStudio;component/assets/glyphs/step.png"));

                var completions = new List<Completion>(_step.GetAll().Select(x => new Completion(x, x, "Step", stepImageSource, "Step")));

                BitmapSource conceptImageSource = new BitmapImage(new Uri("pack://application:,,,/Gauge.VisualStudio;component/assets/glyphs/concept.png"));
                completions.AddRange(_concept.GetAllConcepts().Select(x => new Completion(x.StepValue, x.StepValue, "Concept", conceptImageSource, "Concept")));

                var snapshot = _buffer.CurrentSnapshot;
                var snapshotPoint = session.GetTriggerPoint(snapshot);
                if (snapshotPoint == null) return;

                var triggerPoint = (SnapshotPoint)snapshotPoint;

                var line = triggerPoint.GetContainingLine();
                var start = triggerPoint;

                while (start > line.Start && !char.IsWhiteSpace((start - 1).GetChar()))
                    start -= 1;

                var applicableTo = snapshot.CreateTrackingSpan(new SnapshotSpan(start, triggerPoint), SpanTrackingMode.EdgeInclusive);
                completionSets.Add(new CompletionSet("Gauge", "Gauge",
                    applicableTo, completions, null));
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    GC.SuppressFinalize(this);
                    _disposed = true;
                }
            }
        }
    }
}