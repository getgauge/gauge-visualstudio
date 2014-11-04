using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Gauge.VisualStudio
{
    internal sealed class GaugeContentTypeDefinitions
    {
        public const string GaugeContentType = "Gauge";
        private const string SpecFileExtension = ".spec";
        private const string MarkdownFileExtension = ".md";

        [Export]
        [Name(GaugeContentType)]
        [BaseDefinition("text")]
        internal static ContentTypeDefinition GaugeContentTypeDefinition = null;

        [Export(typeof(ClassificationTypeDefinition))]
        [Name(GaugeContentType)]
        [BaseDefinition("text")]
        internal static ClassificationTypeDefinition GaugeClassificationTypeDefinition = null;

        [Export]
        [FileExtension(SpecFileExtension)]
        [ContentType(GaugeContentType)]
        internal static FileExtensionToContentTypeDefinition GaugeFileExtensionDefinition = null;

        [Export]
        [FileExtension(MarkdownFileExtension)]
        [ContentType(GaugeContentType)]
        internal static FileExtensionToContentTypeDefinition GaugeMarkdownFileExtensionDefinition = null;
    }
}