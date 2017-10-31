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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Gauge.VisualStudio.Core.Extensions;
using Gauge.VisualStudio.Model;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestWindow.Extensibility;

namespace Gauge.VisualStudio.TestAdapter
{
    [Export(typeof(ITestContainerDiscoverer))]
    public class TestContainerDiscoverer : ITestContainerDiscoverer
    {
        private readonly ProjectItemsEvents _projectItemsEvents;
        private readonly IServiceProvider _serviceProvider;

        [ImportingConstructor]
        public TestContainerDiscoverer([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            var dte = _serviceProvider.GetService(typeof(DTE)) as DTE;
            var events2 = (Events2) dte.Events;
            _projectItemsEvents = events2.ProjectItemsEvents;

            _projectItemsEvents.ItemAdded += UpdateTestContainersIfGaugeSpecFile;
            _projectItemsEvents.ItemRemoved += UpdateTestContainersIfGaugeSpecFile;
            _projectItemsEvents.ItemRenamed += (item, s) => UpdateTestContainersIfGaugeSpecFile(item);
        }

        public Uri ExecutorUri => TestExecutor.ExecutorUri;

        public IEnumerable<ITestContainer> TestContainers => GetTestContainers();

        public event EventHandler TestContainersUpdated;

        private IEnumerable<TestContainer> GetTestContainers()
        {
            var testContainers = new ConcurrentBag<TestContainer>();
            var specs = Specification.GetAllSpecsFromGauge();
            Parallel.ForEach(specs, s =>
            {
                testContainers.Add(new TestContainer(this, s, DateTime.Now));
            });
            return testContainers;
        }

        private void UpdateTestContainersIfGaugeSpecFile(ProjectItem projectItem)
        {
            if (projectItem?.ContainingProject == null || projectItem.Document == null)
                return;
            if (projectItem.ContainingProject.IsGaugeProject() 
                && projectItem.Name.IsGaugeSpecFile())
                TestContainersUpdated?.Invoke(this, EventArgs.Empty);
        }
    }
}