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

using System.IO;
using System.Text;
using FakeItEasy;
using Gauge.VisualStudio.Core.Exceptions;
using NUnit.Framework;

namespace Gauge.VisualStudio.Core.Tests
{
    [TestFixture]
    public class GaugeServiceTests
    {
        [Test]
        public void ShouldGetGaugeVersion()
        {
            const string json = "{\"version\": \"0.4.0\",\"plugins\": [{\"name\": \"csharp\",\"version\": \"0.7.3\"},{\"name\": \"html-report\",\"version\": \"2.1.0\"}]}";
            var outputStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));

            var gaugeProcess = A.Fake<IGaugeProcess>();
            A.CallTo(() => gaugeProcess.Start()).Returns(true);
            A.CallTo(() => gaugeProcess.StandardOutput).Returns(new StreamReader(outputStream));
            A.CallTo(() => gaugeProcess.StandardError).Returns(new StreamReader(errorStream));

            var installedGaugeVersion = GaugeService.Instance.GetInstalledGaugeVersion(gaugeProcess);
            Assert.AreEqual("0.4.0", installedGaugeVersion.version);
            Assert.AreEqual(2, installedGaugeVersion.plugins.Length);
        }

        [Test]
        public void ShouldThrowExceptionWhenExitCodeIsNonZero()
        {
            const string errorMessage = "This is an error message";
            var outputStream = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));
            var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(errorMessage));

            var gaugeProcess = A.Fake<IGaugeProcess>();
            A.CallTo(() => gaugeProcess.Start()).Returns(true);
            A.CallTo(() => gaugeProcess.ExitCode).Returns(123);
            A.CallTo(() => gaugeProcess.StandardOutput).Returns(new StreamReader(outputStream));
            A.CallTo(() => gaugeProcess.StandardError).Returns(new StreamReader(errorStream));

            var exception = Assert.Throws<GaugeVersionNotFoundException>(() => GaugeService.Instance.GetInstalledGaugeVersion(gaugeProcess));

            Assert.NotNull(exception);
            Assert.NotNull(exception.Data);
            Assert.AreEqual("Unable to read Gauge version", exception.Message);
            Assert.AreEqual(errorMessage, exception.Data["GaugeError"]);
        }

        [Test]
        public void ShouldBeIncompatibleWithOldGaugeVersion()
        {
            const string json = "{\"version\": \"0.6.2\",\"plugins\": [{\"name\": \"csharp\",\"version\": \"0.9.2\"},{\"name\": \"html-report\",\"version\": \"2.1.0\"}]}";
            var outputStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));
            const string expectedMessage = "This plugin works with Gauge 0.6.3 or above. You have Gauge 0.6.2 installed. Please update your Gauge installation.\n" + 
                " Run gauge -v from your command prompt for installation information.";

            var gaugeProcess = A.Fake<IGaugeProcess>();
            A.CallTo(() => gaugeProcess.Start()).Returns(true);
            A.CallTo(() => gaugeProcess.StandardOutput).Returns(new StreamReader(outputStream));
            A.CallTo(() => gaugeProcess.StandardError).Returns(new StreamReader(errorStream));

            var gaugeVersionIncompatibleException = Assert.Throws<GaugeVersionIncompatibleException>(() => GaugeService.Instance.AssertCompatibility(gaugeProcess));

            Assert.AreEqual(expectedMessage, gaugeVersionIncompatibleException.Data["GaugeError"]);
        }

        [Test]
        public void ShouldBeIncompatibleWithGauge063()
        {
            const string json = "{\"version\": \"0.6.3\",\"plugins\": [{\"name\": \"csharp\",\"version\": \"0.9.2\"},{\"name\": \"html-report\",\"version\": \"2.1.0\"}]}";
            var outputStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var errorStream = new MemoryStream(Encoding.UTF8.GetBytes(string.Empty));

            var gaugeProcess = A.Fake<IGaugeProcess>();
            A.CallTo(() => gaugeProcess.Start()).Returns(true);
            A.CallTo(() => gaugeProcess.StandardOutput).Returns(new StreamReader(outputStream));
            A.CallTo(() => gaugeProcess.StandardError).Returns(new StreamReader(errorStream));

            Assert.DoesNotThrow(() => GaugeService.Instance.AssertCompatibility(gaugeProcess));
        }
    }
}
