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

using EnvDTE;

namespace Gauge.VisualStudio.Models
{
    internal class Implementation
    {
        public CodeFunction Function { get; set; }
        public dynamic StepAttribute { get; set; }

        public bool ContainsFor(string givenText)
        {
            try
            {
                foreach (var arg in StepAttribute.Arguments)
                {
                    string input = arg.Value.ToString().Trim('"');

                    if (Step.GetStepValueFromInput(input) == Step.GetStepValueFromInput(givenText))
                        return true;
                }
            }
            catch 
            {
                return false;
            }
            return false;
        }
    }
}