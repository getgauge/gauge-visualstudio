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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Gauge.VisualStudio.Highlighting
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(GaugeContentTypeDefinitions.GaugeContentType)]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    internal sealed class StepAdornmentFactory : IWpfTextViewCreationListener
    {
        [Export(typeof(AdornmentLayerDefinition))]
        [Name("StepArdornment")]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
        [TextViewRole(PredefinedTextViewRoles.Interactive)]
        public AdornmentLayerDefinition GaugeAdornmentLayer = null;

        [Import]
        private IViewTagAggregatorFactoryService ViewTagAggregatorFactoryService { get; set; }

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        public void TextViewCreated(IWpfTextView textView)
        {
            ITextDocument document;
            if (!TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out document))
                return;

            var tagAggregator = ViewTagAggregatorFactoryService.CreateTagAggregator<UnimplementedStepTag>(textView);

            var stepAdornment = new StepAdornment(textView, tagAggregator);

            document.FileActionOccurred += (sender, args) => stepAdornment.Update();
        }
    }
}