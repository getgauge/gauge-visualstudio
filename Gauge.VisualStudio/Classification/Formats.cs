using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Gauge.VisualStudio.Classification
{
    static class Formats
    {
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "gauge.specification")]
        [Name("gauge.specification")]
        [DisplayName("Gauge Specification")]
        [UserVisible(true)]
        sealed class GaugeSpecificationFormat : ClassificationFormatDefinition
        {
            public GaugeSpecificationFormat()
            {
                FontRenderingSize = 18;
                IsBold = true;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "gauge.scenario")]
        [Name("gauge.scenario")]
        [DisplayName("Gauge Scenario")]
        [UserVisible(true)]
        sealed class GaugeScenarioFormat : ClassificationFormatDefinition
        {
            public GaugeScenarioFormat()
            {
                FontRenderingSize = 16;
                IsBold = true;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "gauge.step")]
        [Name("gauge.step")]
        [DisplayName("Gauge Step")]
        [UserVisible(true)]
        sealed class GaugeStepFormat : ClassificationFormatDefinition
        {
            public GaugeStepFormat()
            {
                IsBold = true;
                ForegroundColor = Colors.Teal;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "gauge.tag")]
        [Name("gauge.tag")]
        [DisplayName("Gauge Tag")]
        [UserVisible(true)]
        sealed class GaugeTagFormat : ClassificationFormatDefinition
        {
            public GaugeTagFormat()
            {
                IsBold = true;
                ForegroundColor = Colors.Gray;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "gauge.tagvalue")]
        [Name("gauge.tagvalue")]
        [DisplayName("Gauge Tag Value")]
        [UserVisible(true)]
        sealed class GaugeTagValueFormat : ClassificationFormatDefinition
        {
            public GaugeTagValueFormat()
            {
                IsBold = true;
                ForegroundColor = Colors.MediumAquamarine;
            }
        }
    }
}
