using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace Gauge.VisualStudio.TestRunner
{
    [ExtensionUri(ExecutorUriString)]
    public class TestExecutor : ITestExecutor
    {
        public const string ExecutorUriString = "executor://gaugespecexecutor/v1";
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);
        private bool _cancelled;

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            _cancelled = false;
            foreach (var testCase in tests)
            {
                if (_cancelled) break;

                var testResult = GaugeRunner.Run(testCase, runContext.IsBeingDebugged);
                frameworkHandle.RecordResult(testResult);
            }
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var testCases = TestDiscoverer.GetSpecs(sources, null);
            RunTests(testCases, runContext, frameworkHandle);
        }

        public void Cancel()
        {
            _cancelled = true;
        }
    }
}