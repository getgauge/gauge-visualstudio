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
using Gauge.VisualStudio.Models;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Gauge.VisualStudio.AutoComplete
{
    internal sealed class GaugeCompletionSet : CompletionSet
    {
        readonly List<Completion> _gaugeCompletions = new List<Completion>();

        public GaugeCompletionSet(SnapshotPoint triggerPoint, Step step, Concept concept)
        {
            var line = triggerPoint.GetContainingLine();

            BitmapSource stepImageSource = new BitmapImage(new Uri("pack://application:,,,/Gauge.VisualStudio;component/assets/glyphs/step.png"));

            var prefix = line.GetText().TrimStart('*').TrimStart(' ');
            var applicableCompletions = prefix.Length < 1 ? step.GetAll() : step.GetAll().Where(s => s.StartsWith(prefix));
            var stepCompletions = applicableCompletions.Select(x => new Completion(x, x, "Step", stepImageSource, "Step"));
            _gaugeCompletions.AddRange(stepCompletions);

            BitmapSource conceptImageSource = new BitmapImage(new Uri("pack://application:,,,/Gauge.VisualStudio;component/assets/glyphs/concept.png"));
            var applicableContextCompletions = prefix.Length < 1 ? concept.GetAllConcepts() : concept.GetAllConcepts().Where(s => s.StepValue.StartsWith(prefix));
            _gaugeCompletions.AddRange(applicableContextCompletions.Select(x => new Completion(x.StepValue, x.StepValue, "Concept", conceptImageSource, "Concept")));

            if (_gaugeCompletions.Count <= 0) return;

            var start = triggerPoint;
            while (start > line.Start && !char.IsWhiteSpace((start - 1).GetChar()))
                start -= 1;

            ApplicableTo = triggerPoint.Snapshot.CreateTrackingSpan(new SnapshotSpan(start, triggerPoint), SpanTrackingMode.EdgeInclusive);
            Moniker = "Gauge";
            DisplayName = "Gauge";
        }

        public override IList<Completion> Completions
        {
            get { return _gaugeCompletions; }
        }
    }
}