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

using FakeItEasy;
using Microsoft.VisualStudio.Text;
using NUnit.Framework;

namespace Gauge.VisualStudio.Model.Tests
{
    [TestFixture]
    public class StepTests
    {
        [Test]
        public void ShouldFindTableInStep()
        {
            var textSnapshotLines = A.CollectionOfFake<ITextSnapshotLine>(2);
            A.CallTo(() => textSnapshotLines[0].LineNumber).Returns(1);
            A.CallTo(() => textSnapshotLines[1].LineNumber).Returns(2);
            A.CallTo(() => textSnapshotLines[0].GetText()).Returns("* Step that takes a table");
            A.CallTo(() => textSnapshotLines[1].GetText()).Returns("    |col1|col2|");
            var textSnapshot = A.Fake<ITextSnapshot>();
            A.CallTo(() => textSnapshot.GetLineFromLineNumber(2)).Returns(textSnapshotLines[1]);
            A.CallTo(() => textSnapshotLines[0].Snapshot).Returns(textSnapshot);

            Assert.True(Step.HasTable(textSnapshotLines[0]));
        }

        [Test]
        public void ShouldFindTableInStepWithSpecialTableParam()
        {
            var snapshotLine = A.Fake<ITextSnapshotLine>();
            A.CallTo(() => snapshotLine.LineNumber).Returns(1);
            A.CallTo(() => snapshotLine.GetText()).Returns("* Step that takes a table <table:foo.csv>");

            Assert.True(Step.HasTable(snapshotLine));
        }

    }
}
