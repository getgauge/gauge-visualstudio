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
using FakeItEasy;
using Microsoft.VisualStudio.Text;
using NUnit.Framework;

namespace Gauge.VisualStudio.Model.Tests
{
    [TestFixture]
    public class StepTests
    {
        private class InlineTableParamTests
        {
            private IList<ITextSnapshotLine> _textSnapshotLines;
            private const string StepText = "Step that takes a table";

            [SetUp]
            public void Setup()
            {
                _textSnapshotLines = A.CollectionOfFake<ITextSnapshotLine>(2);
                A.CallTo(() => _textSnapshotLines[0].LineNumber).Returns(1);
                A.CallTo(() => _textSnapshotLines[1].LineNumber).Returns(2);
                A.CallTo(() => _textSnapshotLines[0].GetText()).Returns(string.Format("* {0}", StepText));
                A.CallTo(() => _textSnapshotLines[1].GetText()).Returns("    |col1|col2|");
                var textSnapshot = A.Fake<ITextSnapshot>();
                A.CallTo(() => textSnapshot.GetLineFromLineNumber(2)).Returns(_textSnapshotLines[1]);
                A.CallTo(() => _textSnapshotLines[0].Snapshot).Returns(textSnapshot);
            }

            [Test]
            public void ShouldFindTableInStep()
            {
                Assert.True(Step.HasTable(_textSnapshotLines[0]));
            }

            [Test]
            public void ShouldFetchStepTextForInlineTable()
            {
                var expected = string.Format("{0} <table>", StepText);
                Assert.AreEqual(expected, Step.GetStepText(_textSnapshotLines[0]));
            }
        }

        private class TableParamTests
        {
            private ITextSnapshotLine _snapshotLine;
            private const string StepText = "Step that takes a table";

            [SetUp]
            public void Setup()
            {
                _snapshotLine = A.Fake<ITextSnapshotLine>();
                A.CallTo(() => _snapshotLine.LineNumber).Returns(1);
                A.CallTo(() => _snapshotLine.GetText()).Returns(string.Format("* {0} <table:foo.csv>", StepText));
            }

            [Test]
            public void ShouldFindTableInStepWithTableParam()
            {
                Assert.True(Step.HasTable(_snapshotLine));
            }

            [Test]
            public void ShouldFetchStepTextForTableParam()
            {
                var expected = string.Format("{0} <table>", StepText);
                Assert.AreEqual(expected, Step.GetStepText(_snapshotLine));
            }
        }

        private class FileParamTests
        {
            private ITextSnapshotLine _snapshotLine;
            private const string StepText = "Step that takes a table";

            [SetUp]
            public void Setup()
            {
                _snapshotLine = A.Fake<ITextSnapshotLine>();
                A.CallTo(() => _snapshotLine.LineNumber).Returns(1);
                A.CallTo(() => _snapshotLine.GetText()).Returns(string.Format("* {0} <file:foo.txt>", StepText));
            }

            [Test]
            public void ShouldFetchStepTextForTableParam()
            {
                var expected = string.Format("{0} <file>", StepText);
                Assert.AreEqual(expected, Step.GetStepText(_snapshotLine));
            }
        }
    }
}
