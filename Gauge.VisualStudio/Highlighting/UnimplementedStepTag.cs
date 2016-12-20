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

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Gauge.VisualStudio.Highlighting
{
    internal class UnimplementedStepTag : AbstractGaugeErrorTag
    {
        public UnimplementedStepTag(ReadOnlyCollection<SmartTagActionSet> actionSets) : base(actionSets)
        {
            ErrorType = "Unimplemented Step";
            ToolTipContent = "Step is not implemented or does not have a public method implementation.\nUse 'Implement Step' option to generate a method, ensure that the implementation is public";
        }
    }
}