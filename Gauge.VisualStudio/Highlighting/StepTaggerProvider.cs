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
using EnvDTE;
using EnvDTE80;
using Gauge.VisualStudio.Core.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Gauge.VisualStudio.Highlighting
{
    [Export(typeof(IViewTaggerProvider))]
    [ContentType(GaugeContentTypeDefinitions.GaugeContentType)]
    [Order(Before = "default")]
    [TagType(typeof(UnimplementedStepTag))]
    [TagType(typeof(DuplicateStepImplementationTag))]
    public class StepTaggerProvider : IViewTaggerProvider
    {
        private readonly Events2 _events2;

        private readonly Dictionary<ITextView, UnimplementedStepTagger> _taggers =
            new Dictionary<ITextView, UnimplementedStepTagger>();

        private readonly CodeModelEvents _codeModelEvents;
        private readonly DocumentEvents _documentEvents;
        private readonly ProjectItemsEvents _projectItemsEvents;

        public StepTaggerProvider()
        {
            if (_events2 != null) return;

            _events2 = GaugePackage.DTE.Events as Events2;
            _codeModelEvents = _events2.CodeModelEvents;
            _projectItemsEvents = _events2.ProjectItemsEvents;
            _documentEvents = _events2.DocumentEvents;

            _codeModelEvents.ElementAdded += element => RefreshUsages();
            _codeModelEvents.ElementChanged += (element, change) => RefreshUsages();
            _codeModelEvents.ElementDeleted += (parent, element) => RefreshUsages();

            _projectItemsEvents.ItemAdded += item => RefreshUsages();
            _projectItemsEvents.ItemRemoved += item => RefreshUsages();
            _projectItemsEvents.ItemRenamed += (item, name) => RefreshUsages();

            _documentEvents.DocumentSaved += document =>
            {
                if (document.IsGaugeConceptFile() || document.IsGaugeSpecFile())
                {
                    RefreshUsages();
                }
            };
        }

        public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
        {
            if (buffer == null || textView == null)
                return null;

            if (buffer != textView.TextBuffer) return null;

            try
            {
                if (!_taggers.ContainsKey(textView))
                    _taggers[textView] = new UnimplementedStepTagger(textView);
                return _taggers[textView] as ITagger<T>;
            }
            catch
            {
                return null;
            }
        }

        private void RefreshUsages()
        {
            var unimplementedStepTaggers = _taggers
                .Where(unimplementedStepTagger => !unimplementedStepTagger.Key.IsClosed).Select(pair => pair.Value);
            foreach (var unimplementedStepTagger in unimplementedStepTaggers)
                unimplementedStepTagger.RaiseLayoutChanged();
        }
    }
}