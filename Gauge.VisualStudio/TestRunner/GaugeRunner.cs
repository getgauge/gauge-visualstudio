using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Process = System.Diagnostics.Process;
using Thread = System.Threading.Thread;

namespace Gauge.VisualStudio.TestRunner
{
    public class GaugeRunner
    {
        public static TestResult Run(TestCase testCase, bool isBeingDebugged)
        {
            var result = new TestResult(testCase);
            var projectRoot = GetProjectRootPath(new FileInfo(testCase.Source).Directory);
            try
            {
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
                        Arguments = string.Format(@"{0}:{1}", GetTestCasePath(testCase, projectRoot), testCase.LineNumber)
                    }
                };
                p.Start();
                
                if (isBeingDebugged)
                {
                    AttachToProcess(p.Id);
                }
                
                var output = p.StandardOutput.ReadToEnd();
                var error = p.StandardError.ReadToEnd();

                p.WaitForExit();

                result.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, output));

                if (p.ExitCode==0)
                {
                    result.Outcome = TestOutcome.Passed;
                }
                else
                {
                    result.ErrorMessage = error;
                    result.Outcome= TestOutcome.Failed;
                }
            }
            catch (Exception ex)
            {
                result.Outcome = TestOutcome.Failed;
                result.ErrorMessage = string.Format("{0}\n{1}", ex.Message, ex.StackTrace);
            }

            return result;
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
                if (directory==null || directory.Parent==null)
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

        private static void AttachToProcess(int processId)
        {
            var runnerProcessId = GetRunnerProcessId(processId);
            if (runnerProcessId == 0) return;
            foreach (EnvDTE.Process process in DTE.Debugger.LocalProcesses)
            {
                if (process.ProcessID != runnerProcessId) continue;
                process.Attach();
                DTE.Debugger.CurrentProcess = process;
            }
        }

        public static DTE DTE
        {
            get
            {
                return Marshal.GetActiveObject("VisualStudio.DTE") as DTE;
            }
        }

        private static int GetRunnerProcessId(int parentProcessId)
        {
            var retries = 0;
            while (retries < 5)
            {
                var mos = new ManagementObjectSearcher(
                        String.Format("Select * From Win32_Process Where ParentProcessID={0} and Name='Gauge.CSharp.Runner.exe'",
                            parentProcessId));
                var processes = mos.Get().Cast<ManagementObject>().Select(mo => Convert.ToInt32(mo["ProcessID"])).ToList();
                if (processes.Any())
                {
                    return processes.First();
                }
                retries++;
                Thread.Sleep(200);
            }
            return 0;
        }
    }

    internal class ConventionViolationException : Exception
    {
        public ConventionViolationException(string message) : base(message)
        {
        }
    }
}