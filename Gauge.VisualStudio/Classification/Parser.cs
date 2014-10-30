using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;

namespace Gauge.VisualStudio.Classification
{
    static class Parser
    {
        internal const char DummyChar = '~';

        private const string LineTerminator = @"\r|\n|\r\n";
        private const string InputCharacter = @"[^\r\n]";
        private const string WhiteSpace = @"[ \t\f]";
        private const string TableIdentifier = @"[|]";

        private static readonly Regex ScenarioHeadingRegex = new Regex(@"(\#\#.*)$", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);
        private static readonly Regex ScenarioHeadingRegexAlt = new Regex(@".+[\n\r]-+", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

        private static readonly Regex SpecHeadingRegex = new Regex(@"(\#.*)$", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);
        private static readonly Regex SpecHeadingRegexAlt = new Regex(@".+[\n\r]=+", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

        private static readonly Regex StepRegex = new Regex(@"\W+(\*.+)");

        private static Regex _tagsRegex = new Regex(string.Format("{0}* tags {1}? \":\" {2}* {3}?", WhiteSpace, WhiteSpace, InputCharacter,
            LineTerminator), RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace);

        public static List<Token> ParseMarkdownParagraph(string text, int offset = 0)
        {
            var tokens = new List<Token>();
            if (text.Trim().Length == 0)
                return tokens;

            tokens.AddRange(ParseSpecs(text));
            tokens.AddRange(ParseScenarios(text));
            tokens.AddRange(ParseSteps(text));
            return tokens;
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
            Tag
        }

        public struct Token
        {
            public Token(TokenType type, Span span) { TokenType = type; Span = span; }

            public TokenType TokenType;
            public Span Span;
        }

        private static IEnumerable<Token> ParseScenarios(string text)
        {
            // Multiple ifs? Somehow I feel this is more explicit than having complex regex.

            var matches = ScenarioHeadingRegex.Matches(text);
            foreach (Match match in matches)
            {
                yield return new Token(TokenType.Scenario, new Span(match.Index, match.Groups[0].Length));
            }

            matches = ScenarioHeadingRegexAlt.Matches(text);
            foreach (Match match in matches)
            {
                yield return new Token(TokenType.Scenario, new Span(match.Index, match.Groups[0].Length));
            }
        }
        private static IEnumerable<Token> ParseSpecs(string text)
        {
            var matches = SpecHeadingRegex.Matches(text);
            foreach (Match match in matches)
            {
                yield return new Token(TokenType.Specification, new Span(match.Index, match.Groups[0].Length));
            }
            matches = SpecHeadingRegexAlt.Matches(text);
            foreach (Match match in matches)
            {
                yield return new Token(TokenType.Specification, new Span(match.Index, match.Groups[0].Length));
            }
        }

        private static IEnumerable<Token> ParseSteps(string text)
        {
            var matches = StepRegex.Matches(text);
            foreach (Match match in matches)
            {
                yield return new Token(TokenType.Step, new Span(match.Index, match.Groups[0].Length));
            }
        }
    }
}
