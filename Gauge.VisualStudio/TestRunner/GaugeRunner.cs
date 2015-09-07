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
using EnvDTE;
using Gauge.VisualStudio.Exceptions;
using Gauge.VisualStudio.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Process = System.Diagnostics.Process;

namespace Gauge.VisualStudio.TestRunner
{
    public class GaugeRunner
    {
        public void Run(TestCase testCase, bool isBeingDebugged, IFrameworkHandle frameworkHandle)
        {
            var result = new TestResult(testCase);
            var projectRoot = GetProjectRootPath(new FileInfo(testCase.Source).Directory);
            try
            {
                var arguments = string.Format(@"--simple-console {0}:{1}", GetTestCasePath(testCase, projectRoot),
                    testCase.LineNumber);
                var p = new Process
                {
                    StartInfo =
                    {
                        WorkingDirectory = projectRoot,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        FileName = "gauge.exe",
                        RedirectStandardError = true,
                        Arguments = arguments,
                    }
                };

                var _dte = DTEHelper.GetCurrent();
                var buildOutputPath = BuildOutputPath(_dte, projectRoot);
                if (_dte.Solution.SolutionBuild.LastBuildInfo != 0 || !BuildOutputExists(_dte, projectRoot))
                {
                    _dte.Solution.SolutionBuild.Build(true);
                }

                p.StartInfo.EnvironmentVariables["gauge_custom_build_path"] = buildOutputPath;

                if (isBeingDebugged)
                {
                    //Gauge CSharp runner will wait for a debugger to be attached, when it finds this env variable set.
                    p.StartInfo.EnvironmentVariables["DEBUGGING"] = "true";
                }

                p.Start();

                if (isBeingDebugged)
                {
                    DTEHelper.AttachToProcess(p.Id);
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

        private static bool BuildOutputExists(_DTE _dte, string projectRoot)
        {
            var buildOutputPath = BuildOutputPath(_dte, projectRoot);
            return Directory.Exists(buildOutputPath) && Directory.EnumerateFileSystemEntries(buildOutputPath).Any();
        }

        private static string BuildOutputPath(_DTE _dte, string projectRoot)
        {
            var activeConfiguration = _dte.Solution.SolutionBuild.ActiveConfiguration.Name;
            var buildOutputPath = string.Format("{0}\\bin\\{1}", projectRoot, activeConfiguration);
            return buildOutputPath;
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
                if (directory.Name.ToLowerInvariant() == "specs")
                {
                    return directory.Parent.FullName;
                }
                directory = directory.Parent;
            }
        }
    }
}
