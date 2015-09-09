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
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Gauge.VisualStudio.Extensions;
using Gauge.VisualStudio.Models;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using IServiceProvider = System.IServiceProvider;

namespace Gauge.VisualStudio.TestRunner
{
    [Export(typeof(ITestContainerDiscoverer))]
    public class TestContainerDiscoverer : ITestContainerDiscoverer
    {
        private List<TestContainer> _cachedTestContainers;
        private DocumentEvents _documentEvents;
        private ProjectItemsEvents _solutionItemEvents;

        [ImportingConstructor]
        public TestContainerDiscoverer([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            var dte = serviceProvider.GetService(typeof(DTE)) as DTE;
            _documentEvents = dte.Events.DocumentEvents;
            _solutionItemEvents = dte.Events.SolutionItemsEvents;

            _solutionItemEvents.ItemAdded += item => UpdateTestContainersIfGaugeSpecFile(item.Document, true);
            _solutionItemEvents.ItemRemoved += item => UpdateTestContainersIfGaugeSpecFile(item.Document, true);
            _solutionItemEvents.ItemRenamed += (item, s) => UpdateTestContainersIfGaugeSpecFile(item.Document, true);
            _documentEvents.DocumentSaved += doc => UpdateTestContainersIfGaugeSpecFile(doc, false);
        }

        private void UpdateTestContainersIfGaugeSpecFile(Document doc, bool refresh)
        {
            if (doc.IsGaugeSpecFile())
                UpdateTestContainers(refresh);
        }

        public Uri ExecutorUri
        {
            get { return TestExecutor.ExecutorUri; }
        }
        
        public IEnumerable<ITestContainer> TestContainers
        {
            get
            {
                if (_cachedTestContainers==null)
                    UpdateTestContainers(true);
                return _cachedTestContainers;
            }
        }

        private void UpdateTestContainers(bool refresh)
        {
            if (refresh)
            {
                var testContainers = new ConcurrentBag<TestContainer>();
                var specs = Specification.GetAllSpecsFromGauge();
                Parallel.ForEach(specs, s => testContainers.Add(new TestContainer(this, s)));
                _cachedTestContainers = testContainers.ToList();
            }
            TestContainersUpdated(this, EventArgs.Empty);
        }

        public event EventHandler TestContainersUpdated;
   }
}