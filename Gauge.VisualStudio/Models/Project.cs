using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EnvDTE;
using Microsoft.CSharp;
using CodeNamespace = EnvDTE.CodeNamespace;

namespace Gauge.VisualStudio.Models
{
    public class Project
    {
        internal static IEnumerable<Implementation> GetGaugeImplementations(EnvDTE.Project containingProject = null)
        {
            var gaugeImplementations = new List<Implementation>();
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

                    var attribute = allAttributes.FirstOrDefault(a => a.FullName == typeof(CSharp.Lib.Attribute.Step).FullName);
                    if (attribute != null)
                        gaugeImplementations.Add(new Implementation { Function = function, StepAttribute = attribute });
                }
            }

            return gaugeImplementations;
        }

        internal static IEnumerable<CodeElement> GetFunctionsForClass(CodeClass codeClass)
        {
            return GetCodeElementsFor(codeClass.Members, vsCMElement.vsCMElementFunction);
        }

        internal static IEnumerable<CodeElement> GetAllClasses(EnvDTE.Project containingProject = null)
        {
            if (containingProject == null)
            {
                containingProject = GaugeDTEProvider.DTE.ActiveDocument.ProjectItem.ContainingProject;
            }
            return GetCodeElementsFor(containingProject.CodeModel.CodeElements, vsCMElement.vsCMElementClass);
        }

        internal static CodeClass FindOrCreateClass(string className, EnvDTE.Project project = null)
        {
            return GetAllClasses().FirstOrDefault(element => element.Name == className) as CodeClass ??
                   AddClass(className, project);
        }
        private static CodeClass AddClass(string className, EnvDTE.Project project = null)
        {
            if (project==null)
            {
                project = GaugeDTEProvider.DTE.ActiveDocument.ProjectItem.ContainingProject;
            }
            var codeDomProvider = CSharpCodeProvider.CreateProvider("CSharp");

            var targetClass = codeDomProvider.CreateValidIdentifier(className);

            var targetNamespace = project.Properties.Item("DefaultNamespace").Value.ToString();

            var codeNamespace = new System.CodeDom.CodeNamespace(targetNamespace);
            codeNamespace.Imports.Add(new CodeNamespaceImport("System"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("Gauge.CSharp.Lib.Attribute"));

            var codeTypeDeclaration = new CodeTypeDeclaration(targetClass) {IsClass = true, TypeAttributes = TypeAttributes.Public};
            codeNamespace.Types.Add(codeTypeDeclaration);
            var codeCompileUnit = new CodeCompileUnit();
            codeCompileUnit.Namespaces.Add(codeNamespace);

            var targetFileName = Path.Combine(Path.GetDirectoryName(project.FullName), string.Format("{0}.cs", targetClass));
            using (var streamWriter = new StreamWriter(targetFileName))
            {
                var options = new CodeGeneratorOptions { BracingStyle = "C"};
                codeDomProvider.GenerateCodeFromCompileUnit(codeCompileUnit, streamWriter, options);
            }

            project.ProjectItems.AddFromFile(targetFileName);
            return GetAllClasses().First(element => element.Name == targetClass) as CodeClass;
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
}