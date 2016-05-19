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
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;

namespace Gauge.VisualStudio.Model.Extensions
{
    public static class ProjectItemExtensions
    {
        public static IEnumerable<string> GetUsingStatements(this ProjectItem instance)
        {
            Func<CodeImport, string> addUsingStatement = import =>
            {
                var startEditPoint = import.GetStartPoint().CreateEditPoint();
                var textPoint = import.GetEndPoint();
                return startEditPoint.GetText(textPoint);
            };

            foreach (CodeElement codeElement in instance.FileCodeModel.CodeElements)
            {
                if (codeElement.Kind == vsCMElement.vsCMElementImportStmt)
                {
                    var import = codeElement as CodeImport;
                    if (import != null)
                    {
                        yield return addUsingStatement(import);
                    }
                }

                if (codeElement.Kind == vsCMElement.vsCMElementNamespace)
                {
                    foreach (CodeElement childCodeElement in codeElement.Children)
                    {
                        var import = childCodeElement as CodeImport;
                        if (import != null)
                        {
                            yield return addUsingStatement(import);
                        }
                    }
                }
            }
        }

        public static void AddUsingStatement(this ProjectItem instance, string usingStatement)
        {
            var codeNamespace = instance.FileCodeModel.CodeElements.OfType<CodeNamespace>().FirstOrDefault();

            if (codeNamespace == null)
                return;

            var fileCodeModel2 = codeNamespace.ProjectItem.FileCodeModel as FileCodeModel2;

            if (fileCodeModel2 != null)
            {
                fileCodeModel2.AddImport(usingStatement);
            }
        }
    }
}
