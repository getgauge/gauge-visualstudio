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

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;

namespace Gauge.VisualStudio.Model
{
    public static class Parser
    {
        internal const char DummyChar = '~';

        private static readonly Regex ScenarioHeadingRegex = new Regex(@"^\s*\#\#(?<heading>.+)[\n\r]+",
            RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);

        private static readonly Regex ScenarioHeadingRegexAlt = new Regex(@"(?<heading>.+)[\n\r]+[\s]*-+",
            RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

        private static readonly Regex SpecHeadingRegex = new Regex(@"^\s*\#(?<heading>.+)[\n\r]*",
            RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

        private static readonly Regex SpecHeadingRegexAlt = new Regex(@"(?<heading>.+)[\n\r][\s]*=+",
            RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

        public static readonly Regex StepRegex =
            new Regex(
                @"[ ]*\*(?<stepText>(([^{}""\<\>\n\r]*)(?<stat>""(?<statValue>.*?)"")*(?<dyn>\<(?<dynValue>(?!(table|file)).*?)\>)*((?<table><table:(?<tableValue>[^>]*)>)|(?<file><file:(?<fileValue>[^>]*)>))?)*)",
                RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);

        private static readonly Regex TagsRegex = new Regex(@"tags\s*:\s*(?<tag>[^(,|\n)]*)(,(?<tag>[^(,|\n)]*))*",
            RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);

        public static readonly Regex TableRegex = new Regex(@"[ ]*\|[\w ]+\|", RegexOptions.Compiled);

        public static List<Token> ParseMarkdownParagraph(string text, int offset = 0)
        {
            var tokens = new List<Token>();
            if (text.Trim().Length == 0)
                return tokens;

            tokens.AddRange(ParseSpecs(text));
            tokens.AddRange(ParseScenarios(text));
            tokens.AddRange(ParseSteps(text));
            tokens.AddRange(ParseTags(text));
            return tokens;
        }

        public static string GetSpecificationName(string text)
        {
            var match = SpecHeadingRegex.Match(text);
            if (match.Success)
                return match.Groups["heading"].Value.Trim();
            match = SpecHeadingRegexAlt.Match(text);
            return match.Success ? match.Groups["heading"].Value.Trim() : string.Empty;
        }

        public static IEnumerable<string> GetScenarios(string text)
        {
            var matches = ScenarioHeadingRegex.Matches(text);
            foreach (var capture in from Match match in matches from Capture capture in match.Groups["heading"].Captures select capture)
            {
                yield return capture.Value.Trim();
            }
            matches = ScenarioHeadingRegexAlt.Matches(text);
            foreach (var capture in from Match match in matches from Capture capture in match.Groups["heading"].Captures select capture)
            {
                yield return capture.Value.Trim();
            }
        }

        public static bool ParagraphContainsMultilineTokens(string text)
        {
            return SpecHeadingRegexAlt.IsMatch(text) || ScenarioHeadingRegexAlt.IsMatch(text);
        }

        public enum TokenType
        {
            Comment,
            Specification, 
            Scenario,
            Step,
            Tag,
            TagValue,
            StaticParameter,
            DynamicParameter,
            TableParameter,
            FileParameter
        }

        public struct Token
        {
            public Token(TokenType type, Span span, string value)
            {
                TokenType = type; 
                Span = span;
                Value = value;
            }

            public TokenType TokenType;
            public Span Span;
            public string Value;
        }

        private static IEnumerable<Token> ParseScenarios(string text)
        {
            // Multiple ifs? Somehow I feel this is more explicit than having complex regex.
            var matches = ScenarioHeadingRegex.Matches(text);
            foreach (Match match in matches)
            {
                yield return new Token(TokenType.Scenario, new Span(match.Index, match.Length), match.Value);
            }

            matches = ScenarioHeadingRegexAlt.Matches(text);
            foreach (Match match in matches)
            {
                yield return new Token(TokenType.Scenario, new Span(match.Index, match.Length), match.Value);
            }
        }

        private static IEnumerable<Token> ParseSpecs(string text)
        {
            var matches = SpecHeadingRegex.Matches(text);
            foreach (Match match in matches)
            {
                yield return new Token(TokenType.Specification, new Span(match.Index, match.Length), match.Value);
            }
            matches = SpecHeadingRegexAlt.Matches(text);
            foreach (Match match in matches)
            {
                yield return new Token(TokenType.Specification, new Span(match.Index, match.Length), match.Value);
            }
        }

        private static IEnumerable<Token> ParseSteps(string text)
        {
            var matches = StepRegex.Matches(text);
            foreach (Match match in matches)
            {
                yield return new Token(TokenType.Step, new Span(match.Index, match.Length), match.Value);

                var types = new Dictionary<string, TokenType>
                {
                    {"stat", TokenType.StaticParameter},
                    {"dyn", TokenType.DynamicParameter},
                    {"table", TokenType.TableParameter},
                    {"file", TokenType.FileParameter}
                };

                foreach (var tokenType in types)
                {
                    var tokenName = tokenType.Key;
                    for (var i = 0; i < match.Groups[tokenName].Captures.Count; i++)
                    {
                        var capture = match.Groups[tokenName].Captures[i];
                        var captureValue = match.Groups[string.Format("{0}Value", tokenName)].Captures[i].Value;
                        yield return new Token(tokenType.Value, new Span(capture.Index, capture.Length), captureValue);
                    }
                }
            }
        }

        private static IEnumerable<Token> ParseTags(string text)
        {
            var matches = TagsRegex.Matches(text);
            foreach (Match match in matches)
            {
                yield return new Token(TokenType.Tag, new Span(match.Index, match.Length), match.Value.Trim());
                foreach (Capture capture in match.Groups["tag"].Captures)
                {
                    yield return new Token(TokenType.TagValue, new Span(capture.Index, capture.Length), capture.Value.Trim());
                }
            }
        }
    }
}
