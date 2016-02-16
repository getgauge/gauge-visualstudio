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

using Gauge.VisualStudio.Core.Extensions;
using NUnit.Framework;

namespace Gauge.VisualStudio.Core.Tests
{
    [TestFixture]
    public class StringExtensionTests
    {
        [Test]
        public void ShouldConvertStringToLiteral()
        {
            const string s = "this is some step text";
            var expected = string.Format("\"{0}\"",s);

            var literal = s.ToLiteral();

            Assert.AreEqual(expected, literal);
        }

        [Test]
        public void ShouldConvertStringToMethodIdentifier()
        {
            const string s = "123 this is some step 456 :, . text <table>";
            const string expected = "ThisIsSomeStep456TextTable";

            var literal = s.ToMethodIdentifier();

            Assert.AreEqual(expected, literal);
        }

        [Test]
        public void ShouldConvertStringToVariableIdentifier()
        {
            const string s = "123 this is some step 456 :, . text <table>";
            const string expected = "thisIsSomeStep456TextTable";

            var literal = s.ToVariableIdentifier();

            Assert.AreEqual(expected, literal);
        }
    }
}
