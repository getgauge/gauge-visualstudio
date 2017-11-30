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

namespace Gauge.VisualStudio.Core
{
    public class GaugeProcess : IGaugeProcess
    {
        public GaugeProcess(ProcessStartInfo startInfo)
        {
            BaseProcess = new Process {StartInfo = startInfo, EnableRaisingEvents = true};
        }

        public StreamReader StandardError => BaseProcess.StandardError;

        public Process BaseProcess { get; }

        public int Id => BaseProcess.Id;

        public int ExitCode => BaseProcess.ExitCode;

        public StreamReader StandardOutput => BaseProcess.StandardOutput;

        public bool Start()
        {
            if (Exited != null)
                BaseProcess.Exited += Exited;
            if (OutputDataReceived != null)
                BaseProcess.OutputDataReceived += OutputDataReceived;
            return BaseProcess.Start();
        }

        public void WaitForExit()
        {
            BaseProcess.WaitForExit();
        }

        public event EventHandler Exited;

        public void Kill()
        {
            BaseProcess.Kill();
        }

        public event DataReceivedEventHandler OutputDataReceived;

        public void BeginOutputReadLine()
        {
            BaseProcess.BeginOutputReadLine();
        }

        public static IGaugeProcess ForVersion()
        {
            return new GaugeProcess(new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = "gauge.exe",
                CreateNoWindow = true,
                Arguments = "version --machine-readable",
                RedirectStandardError = true,
                RedirectStandardOutput = true
            });
        }

        public static IGaugeProcess ForDaemon(string workingDirectory,
            IEnumerable<KeyValuePair<string, string>> environmentVariables)
        {
            var gaugeStartInfo = new ProcessStartInfo
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                FileName = "gauge.exe",
                CreateNoWindow = true,
                Arguments = "daemon",
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            foreach (var environmentVariable in environmentVariables)
                gaugeStartInfo.EnvironmentVariables[environmentVariable.Key] = environmentVariable.Value;
            return new GaugeProcess(gaugeStartInfo);
        }

        public static IGaugeProcess ForFormat(string gaugeFileDirectoryName, string gaugeFileName)
        {
            return new GaugeProcess(new ProcessStartInfo
            {
                WorkingDirectory = gaugeFileDirectoryName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                FileName = "gauge.exe",
                RedirectStandardError = true,
                Arguments = $@"format {gaugeFileName}"
            });
        }

        public static IGaugeProcess ForExecution(string projectRoot, List<string> scenarios,
            string gaugeCustomBuildPath, bool isBeingDebugged, bool isParallelRun)
        {
            var arguments = $@"run {string.Join(" ", scenarios)} --machine-readable";
            if (isParallelRun)
                arguments = $"{arguments} --parallel";
            var processStartInfo = new ProcessStartInfo
            {
                WorkingDirectory = projectRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                FileName = "gauge.exe",
                RedirectStandardError = true,
                Arguments = arguments
            };
            if (!string.IsNullOrEmpty(gaugeCustomBuildPath))
                processStartInfo.EnvironmentVariables["gauge_custom_build_path"] = gaugeCustomBuildPath;

            if (isBeingDebugged)
                processStartInfo.EnvironmentVariables["DEBUGGING"] = "true";

            return new GaugeProcess(processStartInfo);
        }

        public override string ToString()
        {
            var environmentVariables = BaseProcess.StartInfo.EnvironmentVariables.Keys.Cast<string>()
                .Aggregate(string.Empty,
                    (current, environmentVariable) =>
                        $"{current} {environmentVariable}:{BaseProcess.StartInfo.EnvironmentVariables[environmentVariable]}");

            return
                $"gauge.exe - Arguments:{BaseProcess.StartInfo.Arguments}; ENV - {environmentVariables}";
        }
    }
}