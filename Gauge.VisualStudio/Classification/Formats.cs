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
                FontRenderingSize = 22;
                ForegroundColor = Colors.MediumPurple;
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
                FontRenderingSize = 20;
                ForegroundColor = Colors.MediumPurple;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "gauge.step")]
        [Name("gauge.step")]
        [DisplayName("Gauge Step")]
        [UserVisible(true)]
        sealed class MarkdownListFormat : ClassificationFormatDefinition
        {
            public MarkdownListFormat()
            {
                IsBold = true;
                ForegroundColor = Colors.Teal;
            }
        }
    }
}
