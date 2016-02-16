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
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace Gauge.VisualStudio.Core.Extensions
{
    public static class StringExtensions
    {
        public static string ToLiteral(this string input)
        {
            using (var writer = new StringWriter())
            {
                using (var provider = CodeDomProvider.CreateProvider("CSharp"))
                {
                    provider.GenerateCodeFromExpression(new CodePrimitiveExpression(input), writer, null);
                    return writer.ToString();
                }
            }
        }

        public static string ToMethodIdentifier(this string input)
        {
            var identifierRegex = new Regex("[a-zA-Z][a-zA-Z0-9 _]*", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var matches = identifierRegex.Matches(input);
            var identifier = matches.Cast<Match>().Aggregate(string.Empty, (current, match) => string.Concat(current, match.Value));
            return new CultureInfo("en").TextInfo.ToTitleCase(identifier).Replace(" ", "");
        }

        public static string ToVariableIdentifier(this string input)
        {
            var methodIdentifier = ToMethodIdentifier(input);
            return char.ToLowerInvariant(methodIdentifier[0]) + methodIdentifier.Substring(1);
        }
    }
}