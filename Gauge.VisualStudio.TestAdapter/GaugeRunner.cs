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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Gauge.VisualStudio.Core;
using Gauge.VisualStudio.Core.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

namespace Gauge.VisualStudio.TestAdapter
{
    public class GaugeRunner : IGaugeRunner
    {
        private const string ScenarioEndEvent = "scenarioEnd";
        private const string ErrorEvent = "error";
        private const string SuiteEndEvent = "suiteEnd";
        private const string SpecEndEvent = "specEnd";
        private const string OutEvent = "out";
        private readonly IFrameworkHandle _frameworkHandle;
        private readonly bool _isBeingDebugged;
        private readonly List<TestCase> _tests;
        private readonly IGaugeProcess _gaugeProcess;
        private readonly List<TestCase> _pendingTests;
        private bool _gaugeProcessExited;

        public GaugeRunner(IEnumerable<TestCase> tests, bool isBeingDebugged, bool isParallelRun, IFrameworkHandle frameworkHandle)
        {
            _tests = tests.GroupBy(t => t.Source)
                .SelectMany(spec => spec.OrderBy(t => t.LineNumber))
                .ToList();
            _pendingTests = new List<TestCase>(_tests);
            _isBeingDebugged = isBeingDebugged;
            _frameworkHandle = frameworkHandle;
            var projectRoot = _tests.First().GetPropertyValue(TestDiscoverer.GaugeProjectRoot, string.Empty);
            var gaugeCustomBuildPath =
                _tests.First().GetPropertyValue(TestDiscoverer.GaugeCustomBuildPath, string.Empty); var scenarios = new List<string>();
            foreach (var testCase in _tests)
            {
                _frameworkHandle.RecordStart(testCase);
                _frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Executing Test: {testCase}");

                scenarios.Add($"\"{testCase.Source}:{testCase.LineNumber}\"");
            }

            _gaugeProcess = GaugeProcess.ForExecution(projectRoot, scenarios, gaugeCustomBuildPath, _isBeingDebugged, isParallelRun);
            _gaugeProcess.OutputDataReceived += OnOutputDataReceived;
            _gaugeProcess.Exited += exitHandler;
        }

        public void Run()
        {
            try
            {
                _frameworkHandle.SendMessage(TestMessageLevel.Informational,
                    $"Invoking : {_gaugeProcess}");
                _gaugeProcess.Start();
                _gaugeProcess.BeginOutputReadLine();

                if (_isBeingDebugged)
                {
                    DTEHelper.AttachToProcess(_gaugeProcess.Id);
                    _frameworkHandle.SendMessage(TestMessageLevel.Informational,
                        $"Attaching to ProcessID {_gaugeProcess.Id}");
                }
                _waitForGaugeProcessExit();
            }
            catch (Exception ex)
            {
                _frameworkHandle.SendMessage(TestMessageLevel.Error, ex.Message);
                foreach (var testCase in _tests)
                {
                    var result = new TestResult(testCase)
                    {
                        Outcome = TestOutcome.None,
                        ErrorMessage = $"{ex.Message}\n{ex.StackTrace}"
                    };
                    _frameworkHandle.RecordResult(result);
                    _frameworkHandle.RecordEnd(testCase, result.Outcome);
                    _pendingTests.Remove(testCase);
                }
            }
        }

        private void _waitForGaugeProcessExit()
        {
            const int SleepAmount = 100;
            int elapsedTime = 0;
            while (!_gaugeProcessExited)
            {
                elapsedTime += SleepAmount;
                if (elapsedTime > 30000)
                {
                    break;
                }

                Thread.Sleep(SleepAmount);
            }
        }

        public void Cancel()
        {
            _gaugeProcess.Kill();
            foreach (var pendingTest in _pendingTests)
            {
                _frameworkHandle.RecordEnd(pendingTest, TestOutcome.None);
            }
        }

        private void exitHandler(object sender, System.EventArgs e)
        {
            _gaugeProcessExited = true;
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs args)
        {
            var serializer = new DataContractJsonSerializer(typeof(TestExecutionEvent));
            if (args?.Data == null || !args.Data.Trim().StartsWith("{"))
                return;
            string data = EscapeCharacterSequence(args.Data);
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(data)))
            {
                TestExecutionEvent e;
                try
                {
                    e = (TestExecutionEvent)serializer.ReadObject(ms);
                }
                catch (Exception err)
                {
                    _frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Failed to deserialize : {data}\n Error : {err}");
                    return;
                }
                switch (e.EventType)
                {
                    case ErrorEvent:
                        MarkAllTestsFailed(e.Result.Errors);
                        return;
                    case SuiteEndEvent:
                    case SpecEndEvent:
                        if (e.Result.Errors?.Length>0)
                        {
                            MarkAllTestsFailed(e.Result.Errors);
                        }
                        if (e.Result.BeforeHookFailure!=null)
                        {
                            MarkAllTestsFailed(e.Result.BeforeHookFailure);
                        }
                        if (e.Result.AfterHookFailure!=null)
                        {
                            MarkAllTestsFailed(e.Result.AfterHookFailure);
                        }
                        break;
                    case ScenarioEndEvent:
                        var targetTestCase = _tests.FirstOrDefault(t => $"{t.Source}:{t.LineNumber}" == e.Id);
                        if (targetTestCase == null)
                            return;
                        var testResult = new TestResult(targetTestCase) { Outcome = ParseOutcome(e.Result.Status) };
                        if (!string.IsNullOrEmpty(e.Result.Stdout))
                            testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, e.Result.Stdout));
                        if (e.Result.Errors != null && e.Result.Errors.Length > 0)
                        {
                            foreach (var error in e.Result.Errors)
                            {
                                testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory, error.ToString()));
                            }
                        }
                        _frameworkHandle.RecordResult(testResult);
                        _frameworkHandle.RecordEnd(targetTestCase, testResult.Outcome);
                        _pendingTests.Remove(targetTestCase);
                        break;
                    case OutEvent:
                        if (e.Message.Contains("Unable to create Sandbox"))
                        {
                            MarkAllTestsFailed(e.Message);
                            return;
                        }
                        break;
                    default:
                        return;
                }
            }
        }

        private static string EscapeCharacterSequence(string data)
        {
            data = Regex.Replace(data, @"([^\\])\\([^\\])", "$1\\\\$2");
            data = Regex.Replace(data, "\t", "\\t");
            data = Regex.Replace(data, "\n", "\\n");
            data = Regex.Replace(data, "\r", "\\r");
            data = Regex.Replace(data, "\b", "\\b");
            data = Regex.Replace(data, "\f", "\\f");
            data = Regex.Replace(data, "\v", "\\v");
            return data;
        }

        private void MarkAllTestsFailed(params TestExecutionError[] errors)
        {
            foreach (var test in _tests)
            {
                var result = new TestResult(test) {Outcome = TestOutcome.Failed};
                foreach (var testExecutionError in errors)
                    result.Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory,
                        $"{testExecutionError.Text}\n{testExecutionError.StackTrace}"));
                _frameworkHandle.RecordResult(result);
                _frameworkHandle.RecordEnd(test, result.Outcome);
            }
        }

        private void MarkAllTestsFailed(string message)
        {
            foreach (var test in _tests)
            {
                var result = new TestResult(test) { Outcome = TestOutcome.Failed };
                    result.Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory,
                        message));
                    result.Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory,
                    "\nTry running your test after building your project."));
                _frameworkHandle.RecordResult(result);
                _frameworkHandle.RecordEnd(test, result.Outcome);
            }
        }

        private static TestOutcome ParseOutcome(string resStatus)
        {
            switch (resStatus)
            {
                case "pass":
                    return TestOutcome.Passed;
                case "fail":
                    return TestOutcome.Failed;
                case "skip":
                    return TestOutcome.Skipped;
                default:
                    return TestOutcome.None;
            }
        }
    }
}