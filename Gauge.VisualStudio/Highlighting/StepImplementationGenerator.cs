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
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Gauge.CSharp.Lib;
using Gauge.VisualStudio.Core.Extensions;
using Gauge.VisualStudio.Loggers;
using Gauge.VisualStudio.Model;
using Gauge.VisualStudio.Model.Extensions;
using Microsoft.VisualStudio.Text;

namespace Gauge.VisualStudio.Highlighting
{
    public class StepImplementationGenerator
    {
        private readonly EnvDTE.Project _vsProject;
        private readonly IProject _project;
        private readonly IStep _step;

        public StepImplementationGenerator(EnvDTE.Project vsProject, IProject project, IStep step)
        {
            _vsProject = vsProject;
            _step = step;
            _project = project;
        }

        public bool TryGenerateMethodStub(string selectedClass, ITextSnapshotLine containingLine, out CodeClass targetClass, out CodeFunction implementationFunction)
        {
            targetClass = null;
            implementationFunction = null;
            try
            {
                targetClass = _project.FindOrCreateClass(_vsProject, selectedClass);
            }
            catch (ArgumentException ex)
            {
                StatusBarLogger.Log(ex.Message);
                return false;
            }

            if (targetClass == null)
            {
                //TODO: Display error to user?
                return false;
            }

            var functionName = _step.Text.ToMethodIdentifier();
            var functionCount = _project.GetFunctionsForClass(targetClass)
                    .Count(element => string.CompareOrdinal(element.Name, functionName) == 0);
            functionName = functionCount == 0 ? functionName : functionName + functionCount;

            try
            {
                implementationFunction = targetClass.AddFunction(functionName,
                    vsCMFunction.vsCMFunctionFunction, vsCMTypeRef.vsCMTypeRefVoid, -1,
                    vsCMAccess.vsCMAccessPublic, null);

                var parameterList = _step.Parameters;
                parameterList.Reverse();

                if (_step.HasInlineTable)
                {
                    implementationFunction.AddParameter("table", typeof (Table).Name);
                    parameterList.RemoveAt(0);
                }

                foreach (var parameter in parameterList)
                {
                    if (IsSpecialParameter(parameter))
                    {
                        AddSpecialParam(implementationFunction, parameter);
                    }
                    else
                    {
                        var newName = GenerateNewParameterIdentifier(implementationFunction, parameter);
                        implementationFunction.AddParameter(newName, vsCMTypeRef.vsCMTypeRefString);
                    }
                }

                AddStepAttribute(implementationFunction, _step.Text);
            }
            catch
            {
                if (implementationFunction != null)
                    targetClass.RemoveMember(implementationFunction);
                return false;
            }
            finally
            {
                targetClass.ProjectItem.Save();
            }
            return true;
        }


        private static void AddSpecialParam(CodeFunction implementationFunction, string parameter)
        {
            object paramType = vsCMTypeRef.vsCMTypeRefString;
            if (parameter.StartsWith("table:"))
            {
                paramType = typeof(Table).Name;
            }
            var paramValue = GetParamName(parameter.Split(':').Last());
            var variableIdentifier = GenerateNewParameterIdentifier(implementationFunction, paramValue);
            implementationFunction.AddParameter(variableIdentifier, paramType);
        }

        private static bool IsSpecialParameter(string parameter)
        {
            return parameter.StartsWith("file:") || parameter.StartsWith("table:");
        }

        private static string GenerateNewParameterIdentifier(CodeFunction implementationFunction, string parameter)
        {
            var i = implementationFunction.Parameters.Cast<CodeParameter2>()
                    .Count(param => string.CompareOrdinal(param.Name, parameter) == 0);
            var newName = i == 0 ? parameter : parameter + i;
            return newName.ToVariableIdentifier();
        }

        private static void AddStepAttribute(CodeFunction implementationFunction, string stepValue)
        {
            var codeAttribute = implementationFunction.AddAttribute("Step", stepValue.ToLiteral(), -1);

            if (codeAttribute == null)
            {
                throw new ChangeRejectedException("Step Attribute not created");
            }
        }

        private static string GetParamName(string tableName)
        {
            try
            {
                return Path.GetFileNameWithoutExtension(tableName);
            }
            catch
            {
                return tableName;
            }
        }
    }
}