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
using Gauge.VisualStudio.Core.Exceptions;
using Gauge.VisualStudio.Core.Extensions;
using Gauge.VisualStudio.Model.Extensions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using VSLangProj;
using CodeAttributeArgument = EnvDTE80.CodeAttributeArgument;
using CodeNamespace = System.CodeDom.CodeNamespace;

namespace Gauge.VisualStudio.Model
{
    public class Project : IProject
    {
        private readonly DTE _dte;
        private readonly Events2 _events2;
        private readonly CodeModelEvents _codeModelEvents;
        private readonly DocumentEvents _documentEvents;
        private List<Implementation> _implementations;
        private Dictionary<string, Implementation> _implementationMap;
        private Dictionary<string, bool> _implementationDuplicates;
        private readonly ProjectItemsEvents _projectItemsEvents;
        private static Func<EnvDTE.Project> _vsProjectFunc;
        private CommandEvents _commandEvents;

        public Project(Func<EnvDTE.Project> vsProjectFuncFunc)
        {
            _vsProjectFunc = vsProjectFuncFunc;
            _dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            if (_events2 != null) return;

            _events2 = _dte.Events as Events2;
            _codeModelEvents = _events2.CodeModelEvents;
            _commandEvents = _events2.CommandEvents;

            _projectItemsEvents = _events2.ProjectItemsEvents;
            _documentEvents = _events2.DocumentEvents;
            _documentEvents.DocumentSaved += document =>
            {
                if (document.IsGaugeConceptFile())
                {
                    RefreshImplementations();
                }
            };

            _projectItemsEvents.ItemAdded += projectItem => RefreshImplementations();
            _projectItemsEvents.ItemRemoved += projectItem => RefreshImplementations();
            _projectItemsEvents.ItemRenamed += (item, name) => RefreshImplementations();
            _codeModelEvents.ElementAdded += element => RefreshImplementations();
            _codeModelEvents.ElementChanged += (element, change) => RefreshImplementations();
            _codeModelEvents.ElementDeleted += (parent, element) => RefreshImplementations();

            _commandEvents.AfterExecute += (cmdId, id, customIn, customOut) =>
            {
                if ((VSConstants.VSStd2KCmdID) id == VSConstants.VSStd2KCmdID.RENAME)
                {
                    RefreshImplementations();
                }
            };
        }

        private IEnumerable<Implementation> Implementations
        {
            get
            {
                _implementations = _implementations ?? GetGaugeImplementations(_vsProjectFunc.Invoke());
                return _implementations;
            }
        }

        public void RefreshImplementations()
        {
                _implementations = GetGaugeImplementations(_vsProjectFunc.Invoke());
        }

        public Implementation GetStepImplementation(ITextSnapshotLine line)
        {
            if (_implementationMap == null)
                RefreshImplementations();
            Implementation retval = null;
            _implementationMap.TryGetValue(Step.GetStepValue(line), out retval);
            return retval;
        }


        public IEnumerable<CodeElement> GetFunctionsForClass(CodeClass codeClass)
        {
            return GetCodeElementsFor(codeClass.Members, vsCMElement.vsCMElementFunction);
        }

        public CodeClass FindOrCreateClass(string className)
        {
            var containingProject = _vsProjectFunc.Invoke();
            return GetAllClasses(containingProject, false).FirstOrDefault(element => element.Name == className) as CodeClass ??
                   AddClass(className, containingProject);
        }

        public bool HasDuplicateImplementation(ITextSnapshotLine line)
        {
            if (_implementationDuplicates == null)
                RefreshImplementations();
            bool retval;
            return _implementationDuplicates.TryGetValue(Step.GetStepValue(line), out retval) && retval;
        }

        public IEnumerable<string> GetAllStepsForCurrentProject()
        {
            return Implementations
                .Where(implementation => implementation is StepImplementation)
                .Select(implementation => implementation.StepText);
        }

        public EnvDTE.Project VsProject => _vsProjectFunc.Invoke();

        private List<Implementation> GetGaugeImplementations(EnvDTE.Project containingProject)
        {
            var allClasses = GetAllClasses(containingProject);

            var gaugeImplementations = new List<Implementation>();
            gaugeImplementations.AddRange(GetStepImplementations(allClasses));

            try
            {
                gaugeImplementations.AddRange(new Concept(containingProject).GetAllConcepts()
                    .Select(concept => new ConceptImplementation(concept)));
            }
            catch (GaugeApiInitializationException)
            {
                //do nothing, no concept implementations.
            }

            _implementationMap = new Dictionary<string, Implementation>();
            _implementationDuplicates = new Dictionary<string, bool>();
            foreach(var i in gaugeImplementations)
            {
                _implementationMap.Add(i.StepValue, i);
                _implementationDuplicates.Add(i.StepValue, _implementationDuplicates.ContainsKey(i.StepValue));
            }
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

        public IEnumerable<CodeElement> GetAllClasses(EnvDTE.Project containingProject,
            bool includeReferencedProjects = true)
        {
            if (containingProject?.CodeModel == null)
                return Enumerable.Empty<CodeElement>();

            var vsProject = containingProject.Object as VSProject;
            if (vsProject == null)
                return Enumerable.Empty<CodeElement>();

            if (!includeReferencedProjects)
                return GetCodeElementsFor(containingProject.CodeModel.CodeElements, vsCMElement.vsCMElementClass);

            var codeElements = vsProject.References.Cast<Reference>()
                .Where(reference => reference.SourceProject != null)
                .SelectMany(reference => GetAllClasses(reference.SourceProject));
            return GetCodeElementsFor(containingProject.CodeModel.CodeElements, vsCMElement.vsCMElementClass)
                .Concat(codeElements);
        }

        private static CodeClass AddClass(string className, EnvDTE.Project project)
        {
            var codeDomProvider = CodeDomProvider.CreateProvider("CSharp");

            if (!codeDomProvider.IsValidIdentifier(className))
                throw new ArgumentException(string.Format("Invalid Class Name: {0}", className));

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

            var codeNamespace = new CodeNamespace(targetNamespace);
            codeNamespace.Imports.Add(new CodeNamespaceImport("System"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("Gauge.CSharp.Lib"));
            codeNamespace.Imports.Add(new CodeNamespaceImport("Gauge.CSharp.Lib.Attribute"));

            var codeTypeDeclaration =
                new CodeTypeDeclaration(targetClass) {IsClass = true, TypeAttributes = TypeAttributes.Public};
            codeNamespace.Types.Add(codeTypeDeclaration);

            var codeCompileUnit = new CodeCompileUnit();
            codeCompileUnit.Namespaces.Add(codeNamespace);
            var targetFileName = Path.Combine(Path.GetDirectoryName(project.FullName),
                $"{targetClass}.cs");
            using (var streamWriter = new StreamWriter(targetFileName))
            {
                var options = new CodeGeneratorOptions {BracingStyle = "C"};
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
                    codeElements.AddRange(GetCodeElementsFor(((EnvDTE.CodeNamespace) elem).Members, type));
                else if (elem.InfoLocation == vsCMInfoLocation.vsCMInfoLocationExternal)
                    continue;
                else if (elem.IsCodeType)
                    codeElements.AddRange(GetCodeElementsFor(((CodeType) elem).Members, type));
                if (elem.Kind == type)
                    codeElements.Add(elem);
            }

            return codeElements;
        }

        public static void NavigateToFunction(CodeFunction function)
        {
            if (!function.ProjectItem.IsOpen)
                function.ProjectItem.Open();

            var startPoint = function.GetStartPoint(vsCMPart.vsCMPartHeader);
            startPoint.TryToShow();
            startPoint.Parent.Selection.MoveToPoint(startPoint);
        }
    }
}