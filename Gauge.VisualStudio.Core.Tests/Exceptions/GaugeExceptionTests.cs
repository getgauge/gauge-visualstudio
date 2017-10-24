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
using Gauge.VisualStudio.Core.Exceptions;
using NUnit.Framework;

namespace Gauge.VisualStudio.Core.Tests.Exceptions
{
    [TestFixture]
    public class GaugeExceptionTests
    {
        private static object[] _exceptions =
        {
            new object[] { typeof(GaugeVersionIncompatibleException), "GAUGE-VS-002"},
            new object[] { typeof(GaugeVersionNotFoundException), "GAUGE-VS-003"},
        };

        [Test]
        [TestCaseSource(nameof(_exceptions))]
        public void ShouldHaveErrorCode(Type t, string errorCode)
        {
            const string message = "foo message";

            var ex = (GaugeExceptionBase)Activator.CreateInstance(t, message);

            Assert.AreEqual(ex.Data["ErrorCode"], errorCode);
        }

        [Test]
        [TestCaseSource(nameof(_exceptions))]
        public void ShouldHaveDataInString(Type t, string errorCode)
        {
            const string message = "foo message";

            var ex = (GaugeExceptionBase)Activator.CreateInstance(t, message);

            Assert.That($"{ex}".Contains(errorCode), Is.True, $"Expected {ex} to contain {errorCode}");
        }

        [Test]
        [TestCaseSource(nameof(_exceptions))]
        public void ShouldHaveReferenceUrlInString(Type t, string errorCode)
        {
            const string message = "foo message";
            var refUrl = $"https://info.getgauge.io/{errorCode}";

            var ex = (GaugeExceptionBase)Activator.CreateInstance(t, message);

            Assert.That($"{ex}".Contains(refUrl), Is.True, $"Expected {ex} to contain {refUrl}");
        }
    }
}
