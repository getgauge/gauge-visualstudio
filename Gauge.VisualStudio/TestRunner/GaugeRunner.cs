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
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Process = System.Diagnostics.Process;
using Thread = System.Threading.Thread;

namespace Gauge.VisualStudio.TestRunner
{
    public class GaugeRunner
    {
        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);
        public static void Run(TestCase testCase, bool isBeingDebugged, IFrameworkHandle frameworkHandle)
        {
            var result = new TestResult(testCase);
            var projectRoot = GetProjectRootPath(new FileInfo(testCase.Source).Directory);
            try
            {
                var arguments = string.Format(@"--simple-console {0}:{1}", GetTestCasePath(testCase, projectRoot), testCase.LineNumber);
                var p = new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = projectRoot,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow=true,
                        FileName = "gauge.exe",
                        RedirectStandardError = true,
                        Arguments = arguments,
                    }
                };
                if (isBeingDebugged)
                {
                    //Gauge CSharp runner will wait for a debugger to be attached, when it finds this env variable set.
                    p.StartInfo.EnvironmentVariables["DEBUGGING"] = "true";
                }

                p.Start();

                if (isBeingDebugged)
                {
                    AttachToProcess(p.Id);
                }
                var output = p.StandardOutput.ReadToEnd();
                var error = p.StandardError.ReadToEnd();

                p.WaitForExit();

                result.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, output));

                if (p.ExitCode == 0)
                {
                    result.Outcome = TestOutcome.Passed;
                }
                else
                {
                    result.ErrorMessage = error;
                    result.Outcome = TestOutcome.Failed;
                }
            }
            catch (Exception ex)
            {
                result.Outcome = TestOutcome.Failed;
                result.ErrorMessage = string.Format("{0}\n{1}", ex.Message, ex.StackTrace);
            }

            frameworkHandle.RecordResult(result);
        }

        private static string GetTestCasePath(TestCase testCase, string rootPath)
        {
            var sourceUri = new Uri(testCase.Source);
            var rootUri = new Uri(Path.Combine(rootPath, "specs"));

            return rootUri.MakeRelativeUri(sourceUri).ToString();
        }

        private static string GetProjectRootPath(DirectoryInfo directory)
        {
            while (true)
            {
                if (directory == null || directory.Parent == null)
                {
                    throw new ConventionViolationException("Folder structure does not follow Gauge Convention.");
                }
                if (directory.Name == "specs")
                {
                    return directory.Parent.FullName;
                }
                directory = directory.Parent;
            }
        }

        private static void AttachToProcess(int parentProcessId)
        {
            var runnerProcessId = GetRunnerProcessId(parentProcessId);
            if (runnerProcessId == -1) return;
            var dte = GetCurrent();
            foreach (EnvDTE.Process process in dte.Debugger.LocalProcesses)
            {
                if (process.ProcessID != runnerProcessId) continue;
                try
                {
                    process.Attach();
                }
                catch
                {
                    //do nothing
                }
            }
        }
        internal static DTE GetCurrent()
        {
            var testRunnerProcess = Process.GetCurrentProcess();
            if (!"vstest.executionengine.x86".Equals(testRunnerProcess.ProcessName, StringComparison.OrdinalIgnoreCase))
                return null;

            string progId = string.Format("!{0}.DTE.{1}:{2}", "VisualStudio", "12.0", GetVisualStudioProcessId(testRunnerProcess.Id));

            object runningObject = null;

            IBindCtx bindCtx = null;
            IRunningObjectTable rot = null;
            IEnumMoniker enumMonikers = null;

            try
            {
                Marshal.ThrowExceptionForHR(CreateBindCtx(0, out bindCtx));
                bindCtx.GetRunningObjectTable(out rot);
                rot.EnumRunning(out enumMonikers);

                var moniker = new IMoniker[1];
                uint numberFetched;
                while (enumMonikers.Next(1, moniker, out numberFetched) == 0)
                {
                    var runningObjectMoniker = moniker[0];

                    string name = null;

                    try
                    {
                        if (runningObjectMoniker != null)
                        {
                            runningObjectMoniker.GetDisplayName(bindCtx, null, out name);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // do nothing.
                    }

                    if (string.IsNullOrEmpty(name) || !string.Equals(name, progId, StringComparison.Ordinal)) continue;

                    rot.GetObject(runningObjectMoniker, out runningObject);
                    break;
                }
            }
            finally
            {
                if (enumMonikers != null)
                {
                    Marshal.ReleaseComObject(enumMonikers);
                }

                if (rot != null)
                {
                    Marshal.ReleaseComObject(rot);
                }

                if (bindCtx != null)
                {
                    Marshal.ReleaseComObject(bindCtx);
                }
            }

            return (DTE)runningObject;
        }

        private static int GetRunnerProcessId(int parentProcessId)
        {
            var retries = 0;
            while (retries < 5)
            {
                var mos = new ManagementObjectSearcher(string.Format("Select * From Win32_Process Where ParentProcessID={0} and Name='Gauge.CSharp.Runner.exe'", parentProcessId));
                var processes = mos.Get().Cast<ManagementObject>().Select(mo => Convert.ToInt32(mo["ProcessID"])).ToList();
                if (processes.Any())
                {
                    return processes.First();
                }
                retries++;
                Thread.Sleep(200);
            }
            return -1;
        }

        private static int GetVisualStudioProcessId(int testRunnerProcessId)
        {
            var mos = new ManagementObjectSearcher(string.Format("Select * From Win32_Process Where ProcessID={0}",testRunnerProcessId));
            var processes = mos.Get().Cast<ManagementObject>().Select(mo => Convert.ToInt32(mo["ParentProcessID"])).ToList();
            if (processes.Any())
            {
                return processes.First();
            }
            return -1;
        }

    }

    internal class ConventionViolationException : Exception
    {
        public ConventionViolationException(string message) : base(message)
        {
        }
    }
}
