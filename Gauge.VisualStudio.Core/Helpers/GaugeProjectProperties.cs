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

namespace Gauge.VisualStudio.Core.Helpers
{
    [Serializable]
    public class GaugeProjectProperties
    {
        public int ApiPort { get; set; }

        public int DaemonProcessId { get; set; }

        public string BuildOutputPath { get; set; }

        public string ProjectRoot { get; set; }

        public bool UseExecutionAPI { get; set; }
    }
}