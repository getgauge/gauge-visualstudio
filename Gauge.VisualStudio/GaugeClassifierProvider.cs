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
using System.ComponentModel.Composition;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Gauge.VisualStudio
{

    [Export(typeof(IClassifierProvider))]
    [Order(Before = "default")]
    [ContentType(GaugeContentTypeDefinitions.GaugeContentType)]
    internal class GaugeClassifierProvider : IClassifierProvider
    {
        [Import(typeof(SVsServiceProvider))]
        internal IServiceProvider ServiceProvider = null;

        public static readonly Dictionary<string, Dictionary<string, TextPoint>> ConceptDictionary = new Dictionary<string, Dictionary<string, TextPoint>>();

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            return null;
        }
    }
}
