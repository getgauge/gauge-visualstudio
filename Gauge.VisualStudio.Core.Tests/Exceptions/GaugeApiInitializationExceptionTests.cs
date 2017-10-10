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

using Gauge.VisualStudio.Core.Exceptions;
using NUnit.Framework;

namespace Gauge.VisualStudio.Core.Tests.Exceptions
{
    [TestFixture]
    public class GaugeApiInitializationExceptionTests
    {
        private const string ErrorCode = "GAUGE-VS-001";

        [Test]
        public void ShouldHaveErrorCode()
        {
            var ex = new GaugeApiInitializationException("", "");

            Assert.AreEqual(ex.Data["ErrorCode"], ErrorCode);
        }

        [Test]
        public void ShouldHaveDataInString()
        {
            var ex = new GaugeApiInitializationException("", "");

            Assert.That($"{ex}".Contains(ErrorCode), Is.True, $"Expected {ex} to contain {ErrorCode}");
        }

        [Test]
        public void ShouldHaveReferenceUrlInString()
        {
            var refUrl = $"https://info.getgauge.io/{ErrorCode}";

            var ex = new GaugeApiInitializationException("", "");

            Assert.That($"{ex}".Contains(refUrl), Is.True, $"Expected {ex} to contain {refUrl}");
        }
    }
}
