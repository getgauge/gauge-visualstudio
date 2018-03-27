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
using Gauge.VisualStudio.Model.Extensions;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Project = Gauge.VisualStudio.Model.Project;

namespace Gauge.VisualStudio.AutoComplete
{
    public class CompletionSource
    {
        [Export(typeof(ICompletionSourceProvider))]
        [ContentType(GaugeContentTypeDefinitions.GaugeContentType)]
        [Name("gaugeCompletion")]
        private class GaugeCompletionSourceProvider : ICompletionSourceProvider
        {
            [Import]
            internal ITextStructureNavigatorSelectorService NavigatorService { get; set; }

            public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
            {
                try
                {
                    return new GaugeCompletionSource(textBuffer);
                }
                catch
                {
                    return null;
                }
            }
        }

        private class GaugeCompletionSource : ICompletionSource
        {
            private readonly ITextBuffer _buffer;
            private readonly Concept _concept;
            private bool _disposed;
            private readonly IProject _project;

            public GaugeCompletionSource(ITextBuffer buffer)
            {
                var vsProject = buffer.CurrentSnapshot.GetProject(GaugePackage.DTE);
                var vsProjectFunc = new Func<EnvDTE.Project>(() => vsProject);
                _project = ProjectFactory.Get(vsProject);
                _concept = new Concept(vsProjectFunc.Invoke());
                _buffer = buffer;
            }

            public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
            {
                if (_disposed)
                    throw new ObjectDisposedException("GaugeCompletionSource");

                var snapshot = _buffer.CurrentSnapshot;
                var snapshotPoint = session.GetTriggerPoint(snapshot);
                if (!snapshotPoint.HasValue) return;
                completionSets.Add(new GaugeCompletionSet(snapshotPoint.Value, _project, _concept));
            }

            public void Dispose()
            {
                if (_disposed) return;

                GC.SuppressFinalize(this);
                _disposed = true;
            }
        }
    }
}