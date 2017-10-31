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
    public class TestExecutionError
    {
        [DataMember(Name = "text")]
        public string Text { get; set; }

        [DataMember(Name = "filename")]
        public string Filename { get; set; }

        [DataMember(Name = "message")]
        public string Message { get; set; }

        [DataMember(Name = "lineNo")]
        public string LineNo { get; set; }

        [DataMember(Name = "stackTrace")]
        public string StackTrace { get; set; }

        public override string ToString()
        {
            return $"{Text} => {Message}\n at {Filename}:{LineNo}\nStacktrace:\n{StackTrace}";
        }
    }
}