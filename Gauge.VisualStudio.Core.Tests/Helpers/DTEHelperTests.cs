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

using Gauge.VisualStudio.Core.Helpers;
using NUnit.Framework;

namespace Gauge.VisualStudio.Core.Tests.Helpers
{
    [TestFixture]
    public class DTEHelperTests
    {
        [Test]
        public void NonVisualStudioProcessNameShouldBeInvalid()
        {
            Assert.IsFalse(DTEHelper.IsVisualStudioProcessName("!Random.DTE.12.0:", default(int)));
        }

        [Test]
        public void NullValueShouldBeInvalidProcessName()
        {
            Assert.IsFalse(DTEHelper.IsVisualStudioProcessName(null, default(int)));
        }

        [Test]
        public void ProcessNameForVS2013ShouldBeValid()
        {
            Assert.True(DTEHelper.IsVisualStudioProcessName("!VisualStudio.DTE.12.0:1234", default(int)));
        }

        [Test]
        public void ProcessNameForVS2015ShouldBeValid()
        {
            Assert.True(DTEHelper.IsVisualStudioProcessName("!VisualStudio.DTE.14.0:1234", default(int)));
        }
    }
}