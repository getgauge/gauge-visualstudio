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
using Gauge.VisualStudio.Model;
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
            private readonly ITextBuffer _buffer;
            private bool _disposed;
            private readonly Concept _concept = new Concept(GaugePackage.ActiveProject);

            public GaugeCompletionSource(GaugeCompletionSourceProvider gaugeCompletionSourceProvider, ITextBuffer buffer)
            {
                _buffer = buffer;
            }

            public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
            {
                if (_disposed)
                    throw new ObjectDisposedException("GaugeCompletionSource");

                var snapshot = _buffer.CurrentSnapshot;
                var snapshotPoint = session.GetTriggerPoint(snapshot);
                if (!snapshotPoint.HasValue) return;
                var step = new Step(GaugePackage.ActiveProject, snapshotPoint.Value.GetContainingLine());
                completionSets.Add(new GaugeCompletionSet(snapshotPoint.Value, step, _concept));
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