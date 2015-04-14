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
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Gauge.VisualStudio.Classification
{
    static class Types
    {
        [Export]
        [Name("gauge")]
        internal static ClassificationTypeDefinition GaugeBaseClassificationDefinition = null;

        [Export]
        [Name("gauge.header")]
        [BaseDefinition("gauge")]
        internal static ClassificationTypeDefinition GaugeBaseHeaderDefinition = null;

        [Export]
        [Name("gauge.specification")]
        [BaseDefinition("gauge.header")]
        internal static ClassificationTypeDefinition GaugeSpecificationDefinition = null;

        [Export]
        [Name("gauge.scenario")]
        [BaseDefinition("gauge.header")]
        internal static ClassificationTypeDefinition GaugeScenarioDefinition = null;

        [Export]
        [Name("gauge.step")]
        [BaseDefinition("gauge")]
        [BaseDefinition(PredefinedClassificationTypeNames.Keyword)]
        internal static ClassificationTypeDefinition GaugeStepDefinition = null;

        [Export]
        [Name("gauge.static_param")]
        [BaseDefinition("gauge.step")]
        internal static ClassificationTypeDefinition GaugeStaticParamDefinition = null;

        [Export]
        [Name("gauge.dynamic_param")]
        [BaseDefinition("gauge.step")]
        internal static ClassificationTypeDefinition GaugeDynamicParamDefinition = null;

        [Export]
        [Name("gauge.comment")]
        [BaseDefinition("gauge")]
        internal static ClassificationTypeDefinition GaugeCommentDefinition = null;
        
        [Export]
        [Name("gauge.tag")]
        [BaseDefinition("gauge")]
        [BaseDefinition(PredefinedClassificationTypeNames.Comment)]
        internal static ClassificationTypeDefinition GaugeTagDefinition = null;
        
        [Export]
        [Name("gauge.tagvalue")]
        [BaseDefinition("gauge.tag")]
        [BaseDefinition(PredefinedClassificationTypeNames.Literal)]
        internal static ClassificationTypeDefinition GaugeTagValueDefinition = null;
    }
}
