using System;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.OLE.Interop;
using Process = System.Diagnostics.Process;
using Thread = System.Threading.Thread;

namespace Gauge.VisualStudio.Helpers
{
    internal static class DTEHelper
    {
        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

        internal static DTE GetCurrent()
        {
            var testRunnerProcess = Process.GetCurrentProcess();
            if (!"vstest.executionengine.x86".Equals(testRunnerProcess.ProcessName, StringComparison.OrdinalIgnoreCase))
                return null;

            string progId = String.Format("!{0}.DTE.{1}:{2}", "VisualStudio", "12.0", GetVisualStudioProcessId(testRunnerProcess.Id));

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

                    if (String.IsNullOrEmpty(name) || !String.Equals(name, progId, StringComparison.Ordinal)) continue;

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

        internal static int GetRunnerProcessId(int parentProcessId)
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

        internal static void AttachToProcess(int parentProcessId)
        {
            var runnerProcessId = DTEHelper.GetRunnerProcessId(parentProcessId);
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

        private static int GetVisualStudioProcessId(int testRunnerProcessId)
        {
            var mos = new ManagementObjectSearcher(string.Format("Select * From Win32_Process Where ProcessID={0}", testRunnerProcessId));
            var processes = mos.Get().Cast<ManagementObject>().Select(mo => Convert.ToInt32(mo["ParentProcessID"])).ToList();
            if (processes.Any())
            {
                return processes.First();
            }
            return -1;
        }
    }
}
