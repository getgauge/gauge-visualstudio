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

using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Text;

namespace Gauge.VisualStudio.Model.Extensions
{
    public static class TextSnapshotExtensions
    {
        public static EnvDTE.Project GetProject(this ITextSnapshot snapshot, DTE dte)
        {
            ITextDocument textDoc;
            snapshot.TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out textDoc);
            var document = dte.Documents.Cast<Document>()
                .FirstOrDefault(d => string.CompareOrdinal(d.FullName, textDoc.FilePath) == 0);
            return document == null ? null : document.ProjectItem.ContainingProject;
        }
    }
}