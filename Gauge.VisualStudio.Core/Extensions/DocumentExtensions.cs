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

using System.IO;
using System.Linq;
using EnvDTE;

namespace Gauge.VisualStudio.Core.Extensions
{
    public static class DocumentExtensions
    {
        public static bool IsGaugeSpecFile(this Document document)
        {
            return document.FullName.IsGaugeSpecFile();
        }

        public static bool IsGaugeConceptFile(this Document document)
        {
            return document.FullName.IsGaugeConceptFile();
        }

        public static bool IsGaugeSpecFile(this string filePath)
        {
            return File.Exists(filePath) && new[] {".spec", ".md"}.Any(filePath.EndsWith);
        }

        public static bool IsGaugeConceptFile(this string filePath)
        {
            return File.Exists(filePath) && new[] {".cpt"}.Any(filePath.EndsWith);
        }
    }
}