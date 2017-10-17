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

namespace Gauge.VisualStudio.Core
{
    public class GaugeProcess : IGaugeProcess
    {
        public GaugeProcess(ProcessStartInfo startInfo)
        {
            BaseProcess = new Process {StartInfo = startInfo};
        }

        public StreamReader StandardError => BaseProcess.StandardError;

        public Process BaseProcess { get; }

        public int Id => BaseProcess.Id;

        public int ExitCode => BaseProcess.ExitCode;

        public StreamReader StandardOutput => BaseProcess.StandardOutput;

        public bool Start()
        {
            BaseProcess.EnableRaisingEvents = true;
            if (Exited != null)
                BaseProcess.Exited += Exited;

            return BaseProcess.Start();
        }

        public void WaitForExit()
        {
            BaseProcess.WaitForExit();
        }

        public event EventHandler Exited;

        public static IGaugeProcess ForVersion()
        {
            return new GaugeProcess(GetStartInfoForVersion());
        }

        public static IGaugeProcess ForDaemon(string workingDirectory,
            IEnumerable<KeyValuePair<string, string>> environmentVariables)
        {
            return new GaugeProcess(GetStartInfoForDaemon(workingDirectory, environmentVariables));
        }

        private static ProcessStartInfo GetStartInfoForVersion()
        {
            return new ProcessStartInfo
            {
                UseShellExecute = false,
                FileName = "gauge.exe",
                CreateNoWindow = true,
                Arguments = "version --machine-readable",
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
        }

        private static ProcessStartInfo GetStartInfoForDaemon(string workingDirectory,
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
            return gaugeStartInfo;
        }
    }
}