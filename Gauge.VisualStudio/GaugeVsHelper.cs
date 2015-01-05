using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using EnvDTE;

namespace Gauge.VisualStudio
{
    public class GaugeVSHelper
    {
        public static void NavigateToFunction(CodeFunction function)
        {

            if (!function.ProjectItem.IsOpen)
            {
                function.ProjectItem.Open();
            }

            var startPoint = function.GetStartPoint(vsCMPart.vsCMPartHeader);
            startPoint.TryToShow();
            startPoint.Parent.Selection.MoveToPoint(startPoint);
        }
    }

    public static class StringExtensions
    {
        public static string ToLiteral(this string input)
        {
            using (var writer = new StringWriter())
            {
                using (var provider = CodeDomProvider.CreateProvider("CSharp"))
                {
                    provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, null);
                    return writer.ToString();
                }
            }
            
        }
    }
}