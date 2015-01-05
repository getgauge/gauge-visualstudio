using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;

namespace Gauge.VisualStudio.Models
{
    public class GaugeProject
    {
        internal static IEnumerable<GaugeImplementation> GetGaugeImplementations(Project containingProject = null)
        {
            var gaugeImplementations = new List<GaugeImplementation>();
            var allClasses = GetAllClasses(containingProject);

            foreach (var codeElement in allClasses)
            {
                if (!(codeElement is CodeClass)) continue;
                var codeClass = (CodeClass)codeElement;
                var allFunctions = GetFunctionsForClass(codeClass);
                foreach (var codeFunction in allFunctions)
                {
                    var function = codeFunction as CodeFunction;
                    if (function == null) continue;
                    var allAttributes = GetCodeElementsFor(function.Attributes, vsCMElement.vsCMElementAttribute);

                    var attribute = allAttributes.FirstOrDefault(a => a.FullName == "Gauge.CSharp.Lib.Attribute.Step");
                    if (attribute != null)
                        gaugeImplementations.Add(new GaugeImplementation { Function = function, StepAttribute = attribute });
                }
            }

            return gaugeImplementations;
        }

        internal static IEnumerable<CodeElement> GetFunctionsForClass(CodeClass codeClass)
        {
            return GetCodeElementsFor(codeClass.Members, vsCMElement.vsCMElementFunction);
        }

        internal static IEnumerable<CodeElement> GetAllClasses(Project containingProject = null)
        {
            if (containingProject == null)
            {
                containingProject = GaugeDTEProvider.DTE.ActiveDocument.ProjectItem.ContainingProject;
            }
            return GetCodeElementsFor(containingProject.CodeModel.CodeElements, vsCMElement.vsCMElementClass);
        }
        
        private static IEnumerable<CodeElement> GetCodeElementsFor(IEnumerable elements, vsCMElement type)
        {
            var codeElements = new List<CodeElement>();

            foreach (CodeElement elem in elements)
            {
                if (elem.Kind == vsCMElement.vsCMElementNamespace)
                {
                    codeElements.AddRange(GetCodeElementsFor(((CodeNamespace)elem).Members, type));
                }
                else if (elem.InfoLocation == vsCMInfoLocation.vsCMInfoLocationExternal)
                {
                    continue;
                }
                else if (elem.IsCodeType)
                {
                    codeElements.AddRange(GetCodeElementsFor(((CodeType)elem).Members, type));
                }
                if (elem.Kind == type)
                    codeElements.Add(elem);
            }

            return codeElements;
        }         
    }
}