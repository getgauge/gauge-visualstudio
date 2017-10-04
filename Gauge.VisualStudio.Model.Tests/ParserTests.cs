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
using NUnit.Framework;

namespace Gauge.VisualStudio.Model.Tests
{
    [TestFixture]
    public class ParserTests
    {
        public class SpecHeadingTests
        {
            private static readonly TestCaseData[] SpecHeadingTestCases =
            {
                new TestCaseData(string.Empty).Returns(string.Empty),
                new TestCaseData("Spec Heading\n==============").Returns("Spec Heading"),
                new TestCaseData("Spec Heading  \n==============").Returns("Spec Heading"),
                new TestCaseData("Spec Heading  \n  ==============").Returns("Spec Heading"),
                new TestCaseData("  Spec Heading  \n     ==============").Returns("Spec Heading"),
                new TestCaseData("  Spec Heading  \n ").Returns(string.Empty),
                new TestCaseData("foo # Spec Heading  \n ").Returns(string.Empty),
                new TestCaseData("  Spec Heading  \n = ").Returns("Spec Heading"),
                new TestCaseData("  Spec Heading  ==\n = ").Returns("Spec Heading  =="),
                new TestCaseData("# Spec Heading").Returns("Spec Heading"),
                new TestCaseData("  # Spec Heading").Returns("Spec Heading"),
                new TestCaseData("  Spec Heading 123!@$%^&*(){};,.?/|[]==\n = hello all").Returns(
                    "Spec Heading 123!@$%^&*(){};,.?/|[]=="),
                new TestCaseData("  456!@Spec Heading 123!@$%^&*(){};,.?/|[]==\n = hello all").Returns(
                    "456!@Spec Heading 123!@$%^&*(){};,.?/|[]=="),
                new TestCaseData("#Spec Heading\n New Scenario").Returns("Spec Heading"),
                new TestCaseData("#Spec Heading").Returns("Spec Heading"),
                new TestCaseData("#Customer Specification \n ##scenario").Returns("Customer Specification"),
                new TestCaseData("#Spec Heading \n\n ##Scenario Heading \n  * Say \"hello\" to \"gauge\" \n \n ")
                    .Returns("Spec Heading"),
                new TestCaseData(
                        "#My Markdown Spec Heading\n\n##My Markdown scenario Heading\n\n* Say \"hello\" to \"gauge\"")
                    .Returns("My Markdown Spec Heading")
            };

            [Test]
            [TestCaseSource("SpecHeadingTestCases")]
            public string ShouldGetSpecHeadingFromSpecText(string spec)
            {
                return Parser.GetSpecificationName(spec);
            }
        }

        public class ScenarioHeadingTests
        {
            private static readonly TestCaseData[] ScenarioHeadingTestCases =
            {
                new TestCaseData("Spec heading \n ========== \n Scenario Heading \n---------------").Returns(new[]
                    {"Scenario Heading"}),
                new TestCaseData("Spec heading \n ========== \n Scenario Heading \n ---------------").Returns(new[]
                    {"Scenario Heading"}),
                new TestCaseData("#Spec heading \n \n ##Scenario Heading \n").Returns(new[] {"Scenario Heading"}),
                new TestCaseData("#Spec heading \n##Scenario Heading \n").Returns(new[] {"Scenario Heading"}),
                new TestCaseData("#Spec heading \nfoo ##Scenario Heading \n").Returns(Enumerable.Empty<string>()),
                new TestCaseData("#Spec heading\n##Scenario Heading\n##Second Scenario\n").Returns(new[]
                    {"Scenario Heading", "Second Scenario"}),
                new TestCaseData("#Spec heading\n\n ##Scenario Heading\n * Say hi to all \n ##Second Scenario\n")
                    .Returns(new[] {"Scenario Heading", "Second Scenario"}),
                new TestCaseData(
                        "#Spec Heading \n\n" +
                        " ##Scenario Heading \n" +
                        "  * Say \"hello\" to \"gauge\" \n \n" +
                        " ##Second Scenario Heading \n" +
                        " This is second scenario" +
                        "\n Third Scenario\n" +
                        "--------------\n" +
                        " This is third scenario")
                    .Returns(new[] {"Scenario Heading", "Second Scenario Heading", "Third Scenario"}),
                new TestCaseData(
                        "Spec Heading \n" +
                        " ==================\n" +
                        " Scenario Heading \n" +
                        " --------------\n" +
                        " * Say \"hello\" to \"gauge\" \n \n " +
                        "Second Scenario Heading \n" +
                        " --------------\n" +
                        " This is second scenario\n" +
                        " Third Scenario\n" +
                        "--------------\n" +
                        " This is third scenario")
                    .Returns(new[] {"Scenario Heading", "Second Scenario Heading", "Third Scenario"})
            };

            [Test]
            [TestCaseSource("ScenarioHeadingTestCases")]
            public IEnumerable<string> ShouldGiveScenarioHeading(string spec)
            {
                return Parser.GetScenarios(spec);
            }
        }

        public class StepTests
        {
            private static readonly TestCaseData[] ScenarioHeadingTestCases =
            {
                new TestCaseData("* Say hello to gauge").Returns("* Say hello to gauge"),
                new TestCaseData("Say hello to gauge").Returns(string.Empty),
                new TestCaseData("* say \"hello\" to \"gauge\" \n \n  ").Returns("* say \"hello\" to \"gauge\""),
                new TestCaseData("* say \"hello\" and <adjka> to all").Returns("* say \"hello\" and <adjka> to all"),
                new TestCaseData("* say !@ to all").Returns("* say !@ to all"),
                new TestCaseData("* Say \"{hi | bye / how  who ? <>=+-_)(*&^%$#@!~`}\" to \"[hello)\"").Returns(
                    "* Say \"{hi | bye / how  who ? <>=+-_)(*&^%$#@!~`}\" to \"[hello)\""),
                new TestCaseData("* Say ,./?';:\\|][=+-_)(*&^%$#@!~`").Returns("* Say ,./?';:\\|][=+-_)(*&^%$#@!~`"),
                new TestCaseData("* Step that takes a table <table:foo.csv>").Returns(
                    "* Step that takes a table <table:foo.csv>")
            };

            [Test]
            [TestCaseSource("ScenarioHeadingTestCases")]
            public string ShouldGetStepName(string stepText)
            {
                return Parser.StepRegex.Match(stepText).Value.Trim();
            }

            [Test]
            public void ShouldGetTableFromStepText()
            {
                var tokens = Parser.ParseMarkdownParagraph("* Step that takes a table <table:foo.csv>");
                var tableParameter = tokens.First(token => token.TokenType == Parser.TokenType.TableParameter).Value;

                Assert.AreEqual("foo.csv", tableParameter);
            }

            [Test]
            public void ShouldGetMultipleTablesFromStepText()
            {
                var tokens =
                    Parser.ParseMarkdownParagraph(
                        "* Step that takes a table <table:foo.csv> and another table <table:bar.csv>");
                var tableParameters = tokens.Where(token => token.TokenType == Parser.TokenType.TableParameter)
                    .Select(token => token.Value).ToList();

                Assert.AreEqual(new[] {"foo.csv", "bar.csv"}, tableParameters);
            }

            [Test]
            public void ShouldGetFileFromStepText()
            {
                var tokens = Parser.ParseMarkdownParagraph(@"* Step that takes a table <File:c:\blah\foo.txt>");
                var fileParameter = tokens.First(token => token.TokenType == Parser.TokenType.FileParameter).Value;

                Assert.AreEqual(@"c:\blah\foo.txt", fileParameter);
            }

            [Test]
            public void ShouldMatchInlineTable()
            {
                const string tableValue = "    |col1|col2|\n    |value1|value2|";
                var isMatch = Parser.TableRegex.IsMatch(tableValue);

                Assert.IsTrue(isMatch);
            }

            [Test]
            public void ShouldGetStaticParameters()
            {
                var tokens = Parser.ParseMarkdownParagraph("* Say \"hello\" to \"world\"");
                var staticParameters = tokens.Where(token => token.TokenType == Parser.TokenType.StaticParameter)
                    .Select(token => token.Value);

                Assert.AreEqual(new[] {"hello", "world"}, staticParameters);
            }

            [Test]
            public void ShouldGetDynamicParameters()
            {
                var tokens = Parser.ParseMarkdownParagraph("* Say <something> to <someone>");
                var dynamicParameters = tokens.Where(token => token.TokenType == Parser.TokenType.DynamicParameter)
                    .Select(token => token.Value);

                Assert.AreEqual(new[] {"something", "someone"}, dynamicParameters);
            }
        }

        public class TagsTest
        {
            private static readonly TestCaseData[] tagValuesTestCases =
            {
                new TestCaseData("tags: foo, bar").Returns(new[] {"foo", "bar"}),
                new TestCaseData("tags: foo, bar bar , blah,   baz  ").Returns(new[] {"foo", "bar bar", "blah", "baz"}),
                new TestCaseData("tags: a multiword tag, bar").Returns(new[] {"a multiword tag", "bar"}),
                new TestCaseData("  tags: a multiword tag, bar").Returns(new[] {"a multiword tag", "bar"}),
                new TestCaseData("  tags:    foo   ,    bar   ").Returns(new[] {"foo", "bar"}),
                new TestCaseData("  tags: a multiword tag, bar\n this is random text").Returns(new[]
                    {"a multiword tag", "bar"})
            };

            private static readonly TestCaseData[] tagTestCases =
            {
                new TestCaseData("tags: foo, bar").Returns("tags: foo, bar"),
                new TestCaseData("tags: foo, bar , blah,   baz  ").Returns("tags: foo, bar , blah,   baz"),
                new TestCaseData("tags: a multiword tag, bar").Returns("tags: a multiword tag, bar"),
                new TestCaseData("  tags: a multiword tag, bar").Returns("tags: a multiword tag, bar"),
                new TestCaseData("  tags:    foo   ,    bar   ").Returns("tags:    foo   ,    bar"),
                new TestCaseData("  tags: a multiword tag, bar\n this is random text").Returns(
                    "tags: a multiword tag, bar")
            };

            [Test]
            [TestCaseSource("tagValuesTestCases")]
            public IEnumerable<string> ShouldGetTagValues(string input)
            {
                var tokens = Parser.ParseMarkdownParagraph(input);
                return tokens.Where(token => token.TokenType == Parser.TokenType.TagValue).Select(token => token.Value);
            }

            [Test]
            [TestCaseSource("tagTestCases")]
            public string ShouldGetTag(string input)
            {
                var tokens = Parser.ParseMarkdownParagraph(input);
                return tokens.First(token => token.TokenType == Parser.TokenType.Tag).Value;
            }
        }
    }
}