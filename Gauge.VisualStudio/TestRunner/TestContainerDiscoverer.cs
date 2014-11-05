using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestWindow.Extensibility;

namespace Gauge.VisualStudio.TestRunner
{
    [Export(typeof(ITestContainerDiscoverer))]
    public class TestContainerDiscoverer : ITestContainerDiscoverer
    {
        private readonly IServiceProvider _serviceProvider;

        [ImportingConstructor]
        public TestContainerDiscoverer([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Uri ExecutorUri
        {
            get { return TestExecutor.ExecutorUri; }
        }

        public IEnumerable<ITestContainer> TestContainers
        {
            get
            {
                var testContainers = new ConcurrentBag<TestContainer>();
                Parallel.ForEach(GaugeDTEProvider.GetAllSpecs(_serviceProvider),
                    s => testContainers.Add(new TestContainer(this, s)));
                return testContainers;
            }
        }

        public event EventHandler TestContainersUpdated;
    }
}