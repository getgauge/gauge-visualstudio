﻿// Copyright [2014, 2015] [ThoughtWorks Inc.](www.thoughtworks.com)
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

using System.Collections.Generic;
using EnvDTE;
using Microsoft.VisualStudio.Text;
using System;

namespace Gauge.VisualStudio.Model
{
    public interface IProject
    {
        void RefreshImplementations();
        Implementation GetStepImplementation(ITextSnapshotLine line);
        IEnumerable<CodeElement> GetFunctionsForClass(CodeClass codeClass);
        CodeClass FindOrCreateClass(string className);
        bool HasDuplicateImplementation(ITextSnapshotLine line);
        EnvDTE.Project VsProject { get; }
        IEnumerable<Tuple<string, string>> GetAllStepText();
        IEnumerable<CodeElement> GetAllClasses(EnvDTE.Project containingProject,
            bool includeReferencedProjects = true);
    }
}