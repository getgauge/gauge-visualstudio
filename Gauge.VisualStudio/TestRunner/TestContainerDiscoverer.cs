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
using Gauge.VisualStudio.Extensions;
using Gauge.VisualStudio.Models;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestWindow.Extensibility;

namespace Gauge.VisualStudio.TestRunner
{
    [Export(typeof (ITestContainerDiscoverer))]
    public class TestContainerDiscoverer : ITestContainerDiscoverer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DocumentEvents _documentEvents;
        private readonly ProjectItemsEvents _solutionItemEvents;
        private readonly BuildEvents _buildEvents;

        [ImportingConstructor]
        public TestContainerDiscoverer([Import(typeof (SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            var dte = _serviceProvider.GetService(typeof (DTE)) as DTE;
            _documentEvents = dte.Events.DocumentEvents;
            _solutionItemEvents = dte.Events.SolutionItemsEvents;
            _buildEvents = dte.Events.BuildEvents;

            _solutionItemEvents.ItemAdded += item => UpdateTestContainersIfGaugeSpecFile(item.Document);
            _solutionItemEvents.ItemRemoved += item => UpdateTestContainersIfGaugeSpecFile(item.Document);
            _solutionItemEvents.ItemRenamed += (item, s) => UpdateTestContainersIfGaugeSpecFile(item.Document);
            _documentEvents.DocumentSaved += UpdateTestContainersIfGaugeSpecFile;
            _buildEvents.OnBuildDone += (scope, action) =>
            {
                if (action == vsBuildAction.vsBuildActionBuild || action == vsBuildAction.vsBuildActionRebuildAll)
                {
                    RaiseTestContainersUpdated();
                }
            };
        }

        private void RaiseTestContainersUpdated()
        {
            if (TestContainersUpdated != null)
            {
                TestContainersUpdated(this, EventArgs.Empty);
            }
        }

        private void UpdateTestContainersIfGaugeSpecFile(Document doc)
        {
            if (doc.IsGaugeSpecFile())
                RaiseTestContainersUpdated();
        }

        public Uri ExecutorUri
        {
            get { return TestExecutor.ExecutorUri; }
        }

        public IEnumerable<ITestContainer> TestContainers
        {
            get { return GetTestContainers(); }
        }

        private IEnumerable<TestContainer> GetTestContainers()
        {
            var testContainers = new ConcurrentBag<TestContainer>();
            var specs = Specification.GetAllSpecsFromGauge();
            Parallel.ForEach(specs, s => testContainers.Add(new TestContainer(this, s, DateTime.Now)));
            return testContainers;
        }

        public event EventHandler TestContainersUpdated;
    }
}