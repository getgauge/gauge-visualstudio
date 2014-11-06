using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Gauge.VisualStudio.TestRunner
{
    public class GaugeRunner
    {
        public static TestResult Run(TestCase testCase, bool isBeingDebugged)
        {
            //TODO: attach debugger if isBeingDebugged
            var result = new TestResult(testCase);
            var projectRoot = GetProjectRootPath(new FileInfo(testCase.Source).Directory);
            try
            {
                Debug.WriteLine(testCase.LocalExtensionData);
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
            foreach (EnvDTE.Process process in GaugeDTEProvider.DTE.Debugger.LocalProcesses)
            {
                if (process.ProcessID != processId) continue;
                process.Attach();

                GaugeDTEProvider.DTE.Debugger.CurrentProcess = process;
            }
        }
    }



    internal class ConventionViolationException : Exception
    {
        public ConventionViolationException(string message) : base(message)
        {
        }
    }
}