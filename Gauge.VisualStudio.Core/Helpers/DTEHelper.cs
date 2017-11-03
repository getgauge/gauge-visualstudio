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
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.OLE.Interop;
using Process = System.Diagnostics.Process;
using Thread = System.Threading.Thread;

namespace Gauge.VisualStudio.Core.Helpers
{
    public static class DTEHelper
    {
        private static readonly string[] TestRunners = {"vstest.executionengine.x86", "vstest.executionengine", "te.processhost.managed"};

        public static DTE GetCurrent()
        {
            var testRunnerProcess = Process.GetCurrentProcess();

            if (!TestRunners.Contains(testRunnerProcess.ProcessName.ToLower()))
                throw new Exception("Test Runner Process not expected: " + testRunnerProcess.ProcessName.ToLower());

            object runningObject = null;

            IBindCtx bindCtx = null;
            IRunningObjectTable rot = null;
            IEnumMoniker enumMonikers = null;

            try
            {
                Marshal.ThrowExceptionForHR(NativeMethods.CreateBindCtx(0, out bindCtx));
                bindCtx.GetRunningObjectTable(out rot);
                rot.EnumRunning(out enumMonikers);

                var moniker = new IMoniker[1];
                uint numberFetched;
                while (enumMonikers.Next(1, moniker, out numberFetched) == 0)
                {
                    var runningObjectMoniker = moniker[0];

                    string parentProcessName = null;

                    try
                    {
                        if (runningObjectMoniker != null)
                            runningObjectMoniker.GetDisplayName(bindCtx, null, out parentProcessName);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // do nothing.
                    }

                    var isVSProcess = IsVisualStudioProcessName(parentProcessName,
                        GetVisualStudioProcessId(testRunnerProcess.Id));

                    if (!isVSProcess) continue;

                    rot.GetObject(runningObjectMoniker, out runningObject);
                    break;
                }
            }
            finally
            {
                if (enumMonikers != null)
                    Marshal.ReleaseComObject(enumMonikers);

                if (rot != null)
                    Marshal.ReleaseComObject(rot);

                if (bindCtx != null)
                    Marshal.ReleaseComObject(bindCtx);
            }

            return (DTE) runningObject;
        }

        public static bool IsVisualStudioProcessName(string name, int visualStudioProcessId)
        {
            return !string.IsNullOrEmpty(name) && name.Contains("VisualStudio.DTE");
        }

        internal static int GetRunnerProcessId(int parentProcessId)
        {
            var retries = 0;
            while (retries < 5)
            {
                var mos = new ManagementObjectSearcher(
                    $"Select * From Win32_Process Where ParentProcessID={parentProcessId} and Name='Gauge.CSharp.Runner.exe'");
                var processes = mos.Get().Cast<ManagementObject>().Select(mo => Convert.ToInt32(mo["ProcessID"]))
                    .ToList();
                if (processes.Any())
                    return processes.First();
                retries++;
                Thread.Sleep(200);
            }
            return -1;
        }

        public static void AttachToProcess(int parentProcessId)
        {
            var runnerProcessId = GetRunnerProcessId(parentProcessId);
            if (runnerProcessId == -1) return;
            var dte = GetCurrent();
            var processes = dte.Debugger.LocalProcesses.OfType<EnvDTE.Process>();
            var process = processes.FirstOrDefault(proc => proc.ProcessID == runnerProcessId);
            process?.Attach();
        }

        public static void DetachAllProcess()
        {
            GetCurrent().Debugger.DetachAll();
        }

        private static int GetVisualStudioProcessId(int testRunnerProcessId)
        {
            var mos = new ManagementObjectSearcher($"Select * From Win32_Process Where ProcessID={testRunnerProcessId}");
            var processes = mos.Get().Cast<ManagementObject>().Select(mo => Convert.ToInt32(mo["ParentProcessID"]))
                .ToList();
            if (processes.Any())
                return processes.First();
            return -1;
        }
    }
}