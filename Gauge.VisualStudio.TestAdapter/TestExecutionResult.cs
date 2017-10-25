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

using System.Runtime.Serialization;

namespace Gauge.VisualStudio.TestAdapter
{
    [DataContract]
    public class TestExecutionResult
    {
        [DataMember(Name = "status")]
        public string Status { get; set; }

        [DataMember(Name = "time")]
        public long Timestamp { get; set; }

        [DataMember(Name = "out")]
        public string Stdout { get; set; }

        [DataMember(Name = "errors")]
        public TestExecutionError[] Errors { get; set; }

        [DataMember(Name = "beforeHookFailure")]
        public TestExecutionError BeforeHookFailure { get; set; }

        [DataMember(Name = "afterHookFailure")]
        public TestExecutionError AfterHookFailure { get; set; }

        [DataMember(Name="table")]
        public TableInfo Table { get; set; }
    }
}