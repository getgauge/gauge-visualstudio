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
using Microsoft.VisualStudio.Text.Tagging;

namespace Gauge.VisualStudio.Highlighting
{
    internal abstract class AbstractGaugeErrorTag : SmartTag, IErrorTag
    {
        public string ErrorType { get; protected set; }
        public object ToolTipContent { get; protected set; }

        public AbstractGaugeErrorTag(ReadOnlyCollection<SmartTagActionSet> actionSets) : base(SmartTagType.Ephemeral, actionSets)
        {
        }
    }
}