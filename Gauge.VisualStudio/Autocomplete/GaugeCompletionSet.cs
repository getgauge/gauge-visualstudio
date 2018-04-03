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
using System.Linq;
using System.Windows.Media.Imaging;
using Gauge.VisualStudio.Model;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Gauge.VisualStudio.AutoComplete
{
    internal sealed class GaugeCompletionSet : CompletionSet
    {
        private readonly List<Completion> _gaugeCompletions = new List<Completion>();

        public GaugeCompletionSet(SnapshotPoint triggerPoint, IProject project)
        {
            var line = triggerPoint.GetContainingLine();
            var prefix = line.GetText().TrimStart('*', ' ');
            var completions = project.GetAllStepText();
            var applicableCompletions = prefix.Length < 1
                ? completions
                : completions.Where(c => c.Item2.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            if (applicableCompletions.Count() <= 0) return;
            BitmapSource stepImageSource =
                new BitmapImage(new Uri("pack://application:,,,/Gauge.VisualStudio;component/assets/glyphs/step.png"));
            BitmapSource conceptImageSource =
                new BitmapImage(
                    new Uri("pack://application:,,,/Gauge.VisualStudio;component/assets/glyphs/concept.png"));

            _gaugeCompletions.AddRange(applicableCompletions.Select(c => {
                if (c.Item1 == "Step")
                {
                    return new Completion(c.Item2, c.Item2, "Step", stepImageSource, "Step");
                }
                return new Completion(c.Item2, c.Item2, "Concept", conceptImageSource, "Concept");
            }));

            var start = triggerPoint;
            while (start > line.Start && !char.IsWhiteSpace((start - 1).GetChar()))
                start -= 1;

            ApplicableTo = triggerPoint.Snapshot.CreateTrackingSpan(new SnapshotSpan(start, triggerPoint),
                SpanTrackingMode.EdgeInclusive);
            Moniker = "Gauge";
            DisplayName = "Gauge";
        }

        public override IList<Completion> Completions => _gaugeCompletions;
    }
}