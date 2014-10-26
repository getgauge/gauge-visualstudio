using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Gauge.VisualStudio
{
    internal static class GaugeFileContentType
    {
        [Export]
        [Name("spec")]
        [BaseDefinition("text")]
        internal static ContentTypeDefinition GaugeContentTypeDefinition;

        [Export]
        [FileExtension(".spec")]
        [ContentType("spec")]
        internal static FileExtensionToContentTypeDefinition GaugeFileExtensionDefinition;
    }
}