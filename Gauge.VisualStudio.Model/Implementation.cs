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

using System;
using EnvDTE;

namespace Gauge.VisualStudio.Model
{
    public abstract class Implementation
    {
        public string StepText;

        internal bool ContainsImplememntationFor(EnvDTE.Project project, string givenText)
        {
            try
            {
                var gaugeServiceClient = new GaugeServiceClient();
                return string.Compare(gaugeServiceClient.GetParsedStepValueFromInput(project, StepText),
                           gaugeServiceClient.GetParsedStepValueFromInput(project, givenText),
                           StringComparison.Ordinal) == 0;
            }
            catch
            {
                return false;
            }
        }

        public abstract void NavigateToImplementation(DTE dte);
    }
}