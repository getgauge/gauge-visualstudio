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

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Gauge.VisualStudio
{
    internal sealed class GaugeContentTypeDefinitions
    {
        public const string GaugeContentType = "Gauge";
        internal const string SpecFileExtension = ".spec";
        internal const string MarkdownFileExtension = ".md";
        internal const string ConceptFileExtension = ".cpt";

        [Export] [Name(GaugeContentType)] [BaseDefinition("text")]
        internal static ContentTypeDefinition GaugeContentTypeDefinition;

        [Export(typeof(ClassificationTypeDefinition))] [Name(GaugeContentType)] [BaseDefinition("text")]
        internal static ClassificationTypeDefinition GaugeClassificationTypeDefinition;

        [Export] [ContentType(GaugeContentType)]
        internal static FileExtensionToContentTypeDefinition GaugeFileExtensionDefinition;

        [Export] [ContentType(GaugeContentType)]
        internal static FileExtensionToContentTypeDefinition GaugeMarkdownFileExtensionDefinition;

        [Export] [ContentType(GaugeContentType)]
        internal static FileExtensionToContentTypeDefinition GaugeConceptFileExtensionDefinition;
    }
}