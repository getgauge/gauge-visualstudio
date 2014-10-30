using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Gauge.VisualStudio.Classification
{
    [Export(typeof(IClassifierProvider))]
    [ContentType(GaugeContentTypeDefinitions.GaugeContentType)]
    class ClassifierProvider : IClassifierProvider
    {
        [Import]
        IClassificationTypeRegistryService _classificationRegistry;

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new Classifier(buffer, _classificationRegistry));
        }
    }
}
