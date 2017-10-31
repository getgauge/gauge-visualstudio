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
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Microsoft.VisualStudio.TestWindow.Extensibility.Model;

namespace Gauge.VisualStudio.TestAdapter
{
    public class TestContainer : ITestContainer
    {
        private DateTime _timestamp;

        public TestContainer(ITestContainerDiscoverer testContainerDiscoverer, string source, DateTime timestamp)
        {
            Discoverer = testContainerDiscoverer;
            Source = source;
            _timestamp = timestamp;
        }

        public IDeploymentData DeployAppContainer()
        {
            return null;
        }

        public int CompareTo(ITestContainer other)
        {
            var testContainer = other as TestContainer;
            if (testContainer == null)
                return -1;

            var result = string.Compare(Source, testContainer.Source, StringComparison.OrdinalIgnoreCase);
            return result != 0 ? result : _timestamp.CompareTo(testContainer._timestamp);
        }

        public ITestContainer Snapshot()
        {
            return new TestContainer(Discoverer, Source, _timestamp);
        }

        public ITestContainerDiscoverer Discoverer { get; }

        public string Source { get; }

        public IEnumerable<Guid> DebugEngines => Enumerable.Empty<Guid>();

        public FrameworkVersion TargetFramework => FrameworkVersion.None;

        public Architecture TargetPlatform => Architecture.AnyCPU;

        public bool IsAppContainerTestContainer => false;
    }
}