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
using NUnit.Framework;

namespace Gauge.VisualStudio.Core.Tests
{
    [TestFixture]
    public class GaugeVersionTests
    {
        [Test]
        public void ShouldCreateVersionFromString()
        {
            var gaugeVersion = new GaugeVersion("0.1.2");

            Assert.AreEqual(0, gaugeVersion.Major);
            Assert.AreEqual(1, gaugeVersion.Minor);
            Assert.AreEqual(2, gaugeVersion.Patch);
        }

        [Test]
        [TestCase("random_string")]
        [TestCase("123")]
        [TestCase("0..2")]
        [TestCase("-121")]
        public void ShouldThrowArgumentExceptionForInvalidString(string version)
        {
            var argumentException = Assert.Throws<ArgumentException>(() => new GaugeVersion(version));

            Assert.AreEqual(string.Format("Invalid version specified : '{0}'", version), argumentException.Message);
        }

        [Test]
        [TestCase("0.1.2")]
        [TestCase("0.1.2.nightly-2016-12-06")]
        public void ShouldGetToString(string version)
        {
            var actual = new GaugeVersion(version).ToString();

            Assert.AreEqual(version, actual);
        }

        [Test]
        [TestCase("0.1.2.nightly-2016-12-06", true)]
        [TestCase("0.1.2", false)]
        public void ShouldParseNightly(string version, bool expectedNightly)
        {
            var isNightly = new GaugeVersion(version).IsNightly;

            Assert.AreEqual(expectedNightly, isNightly);
        }

        [Test]
        [TestCase("0.1.2.nightly-2016-12-06", "2016-12-06")]
        [TestCase("0.1.2", "0001-01-01")]
        public void ShouldParseNightlyDate(string version, string expectedDate)
        {
            var date = new GaugeVersion(version).Date.ToString("yyyy-MM-dd");

            Assert.AreEqual(expectedDate, date);
        }

        [Test]
        [TestCase("0.1.2", "0.1.5", -1)]
        [TestCase("0.1.2", "0.0.8", 1)]
        [TestCase("0.1.2", "0.1.2", 0)]
        [TestCase("0.1.2.nightly-2015-11-10", "0.1.2.nightly-2015-11-10", 0)]
        [TestCase("0.1.2.nightly-2015-11-10", "0.1.2.nightly-2014-11-10", 1)]
        [TestCase("0.1.2.nightly-2015-11-10", "0.1.1.nightly-2014-11-10", 1)]
        [TestCase("0.1.2.nightly-2015-11-10", "0.1.1.nightly-2015-11-10", 1)]
        [TestCase("0.1.2.nightly-2015-11-10", "0.1.3.nightly-2014-11-10", -1)]
        [TestCase("0.1.2.nightly-2015-11-10", "0.1.2.nightly-2015-12-10", -1)]
        [TestCase("0.1.2.nightly-2015-11-10", "0.1.3.nightly-2015-12-10", -1)]
        [TestCase("0.1.2.nightly-2015-11-10", "0.1.1", 1)]
        [TestCase("0.1.2.nightly-2015-11-10", "0.1.3", -1)]
        public void ShouldCompareTwoGaugeVersions(string v1, string v2, int expected)
        {
            var actual = new GaugeVersion(v1).CompareTo(new GaugeVersion(v2));

            Assert.AreEqual(expected, actual);
        }
    }
}