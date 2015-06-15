using System.Linq;
using EnvDTE;

namespace Gauge.VisualStudio.Extensions
{
    static class DocumentExtensions
    {
        public static bool IsGaugeSpecFile(this Document document)
        {
            return new[] {".spec", ".cpt"}.Any(document.Name.EndsWith);
        } 
    }
}
