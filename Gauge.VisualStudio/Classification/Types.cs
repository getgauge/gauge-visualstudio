using System.ComponentModel.Composition;
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
        internal static ClassificationTypeDefinition GaugeTagDefinition = null;
        
        [Export]
        [Name("gauge.tagvalue")]
        [BaseDefinition("gauge.tag")]
        internal static ClassificationTypeDefinition GaugeTagValueDefinition = null;
    }
}
