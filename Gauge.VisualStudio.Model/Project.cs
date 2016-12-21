// Copyright [2014, 2015] [ThoughtWorks Inc.](www.thoughtworks.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EnvDTE;
using EnvDTE80;
using Gauge.VisualStudio.Core.Extensions;
using Gauge.VisualStudio.Model.Extensions;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using VSLangProj;
using CodeAttributeArgument = EnvDTE80.CodeAttributeArgument;
using CodeNamespace = EnvDTE.CodeNamespace;

namespace Gauge.VisualStudio.Model
{
    public class Project : IProject
    {
        private readonly Events2 _events2;
        private CodeModelEvents _codeModelEvents;
        private List<Implementation> _implementations;
        private DocumentEvents _documentEvents;
        private ProjectItemsEvents _projectItemsEvents;
        private readonly DTE _dte;

        private static readonly Lazy<IProject> LazyInstance = new Lazy<IProject>(() => new Project());

        private Project()
        {
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            if (_events2 != null) return;

            _events2 = _dte.Events as Events2;
            _codeModelEvents = _events2.CodeModelEvents;

            _projectItemsEvents = _events2.ProjectItemsEvents;
            _documentEvents = _events2.DocumentEvents;
            _documentEvents.DocumentSaved += document => RefreshImplementations(document.ProjectItem);
            _projectItemsEvents.ItemAdded += RefreshImplementations;
            _projectItemsEvents.ItemRemoved += RefreshImplementations;
            _projectItemsEvents.ItemRenamed += (item, name) => RefreshImplementations(item);
            _codeModelEvents.ElementAdded += element => RefreshImplementations(element.ProjectItem);
            _codeModelEvents.ElementChanged += (element, change) => RefreshImplementations(element.ProjectItem);
            _codeModelEvents.ElementDeleted += (parent, element) => RefreshImplementations(element.ProjectItem);
        }

        public static IProject Instance
        {
            get { return LazyInstance.Value; }
        }

        public void RefreshImplementations(ProjectItem projectItem)
        {
            if (projectItem.ContainingProject.IsGaugeProject())
            {
                _implementations = GetGaugeImplementations(projectItem.ContainingProject);
            }
        }

        private IEnumerable<Implementation> Implementations
        {
            get
            {
                if (_dte.ActiveDocument!=null)
                {
                    _implementations = _implementations ?? GetGaugeImplementations(_dte.ActiveDocument.ProjectItem.ContainingProject);
                }
                return _implementations;
            }
        }

        public void RefreshImplementationsForActiveProject()
        {
            var activeDocument = _dte.ActiveDocument;
            if (activeDocument != null)
            {
                _implementations = GetGaugeImplementations(activeDocument.ProjectItem.ContainingProject);
            }
        }

        private List<Implementation> GetGaugeImplementations(EnvDTE.Project containingProject)
        {
            var allClasses = GetAllClasses(containingProject);

            var gaugeImplementations = new List<Implementation>();
            gaugeImplementations.AddRange(GetStepImplementations(allClasses));

            gaugeImplementations.AddRange(new Concept(containingProject).GetAllConcepts().Select(concept => new ConceptImplementation(concept)));

            return gaugeImplementations;
        }

        private IEnumerable<StepImplementation> GetStepImplementations(IEnumerable<CodeElement> allClasses)
        {
            var gaugeImplementations = new List<StepImplementation>();
            foreach (var codeElement in allClasses)
            {
                if (!(codeElement is CodeClass)) continue;
                var codeClass = (CodeClass) codeElement;
                var allFunctions = GetFunctionsForClass(codeClass);
                foreach (var codeFunction in allFunctions)
                {
                    var function = codeFunction as CodeFunction;
                    if (function == null || function.Access != vsCMAccess.vsCMAccessPublic) continue;
                    var allAttributes = GetCodeElementsFor(function.Attributes, vsCMElement.vsCMElementAttribute);

                    var attribute = allAttributes.FirstOrDefault(a => a.Name == "Step") as CodeAttribute;

                    if (attribute == null) continue;

                    var stepImplementations = from CodeAttributeArgument argument in attribute.Children
                        where argument != null
                        select new StepImplementation(function, argument.Value.Trim('"'));

                    gaugeImplementations.AddRange(stepImplementations);
                }
            }
            return gaugeImplementations;
        }

        public Implementation GetStepImplementation(ITextSnapshotLine line)
        {
            if (Implementations==null)
            {
                return null;
            }
            try
            {
                var project = line.Snapshot.GetProject(_dte);
                return project == null ? null : Implementations.FirstOrDefault(implementation => implementation.ContainsImplememntationFor(project, Step.GetStepText(line)));
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }


        public IEnumerable<CodeElement> GetFunctionsForClass(CodeClass codeClass)
        {
            return GetCodeElementsFor(codeClass.Members, vsCMElement.vsCMElementFunction);
        }

        public static IEnumerable<CodeElement> GetAllClasses(EnvDTE.Project containingProject, bool includeReferencedProjects = true)
        {
            if (containingProject.CodeModel == null)
                return Enumerable.Empty<CodeElement>();

            var vsProject = containingProject.Object as VSProject;
            if (vsProject == null)
                return Enumerable.Empty<CodeElement>();

            if (!includeReferencedProjects)
                return GetCodeElementsFor(containingProject.CodeModel.CodeElements, vsCMElement.vsCMElementClass);

            var codeElements = vsProject.References.Cast<Reference>()
                .Where(reference => reference.SourceProject != null)
                .SelectMany(reference => GetAllClasses(reference.SourceProject));
            return GetCodeElementsFor(containingProject.CodeModel.CodeElements, vsCMElement.vsCMElementClass).Concat(codeElements);
        }

        public CodeClass FindOrCreateClass(EnvDTE.Project project, string className)
        {
            return GetAllClasses(project, false).FirstOrDefault(element => element.Name == className) as CodeClass ??
                   AddClass(className, project);
        }

        public bool HasDuplicateImplementation(ITextSnapshotLine line)
        {
            try
            {
                var project = line.Snapshot.GetProject(_dte);
                return Implementations.Count(implementation => implementation.ContainsImplememntationFor(project, Step.GetStepText(line))) > 1;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static CodeClass AddClass(string className, EnvDTE.Project project)
        {
            var codeDomProvider = CodeDomProvider.CreateProvider("CSharp");

            if (!codeDomProvider.IsValidIdentifier(className))
            {
                throw new ArgumentException(string.Format("Invalid Class Name: {0}", className));
            }
            
            var targetClass = codeDomProvider.CreateValidIdentifier(className);


            string targetNamespace;
            try
            {
                targetNamespace = project.Properties.Item("DefaultNamespace").Value.ToString();
            }
            catch
            {
                targetNamespace = project.FullName;
            }

            var codeNamespace = new System.CodeDom.CodeNamespace(targetNamespace);
            codeNamespace.Imports.Add(new CodeNamespaceImport("System"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("Gauge.CSharp.Lib"));
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

            var file = project.ProjectItems.AddFromFile(targetFileName);

            var classes = GetCodeElementsFor(file.FileCodeModel.CodeElements, vsCMElement.vsCMElementClass).ToList();
            return classes.First(element => element.Name == targetClass) as CodeClass;
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

        public IEnumerable<string> GetAllStepsForCurrentProject()
        {
            return Implementations
                .Where(implementation => implementation is StepImplementation)
                .Select(implementation => implementation.StepText);
        }
    }
}