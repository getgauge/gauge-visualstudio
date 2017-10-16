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

using System.ComponentModel;
using Gauge.VisualStudio.Core;
using Microsoft.VisualStudio.Shell;

namespace Gauge.VisualStudio
{
    public class GaugeDaemonOptions : DialogPage
    {
        private const int MinimumPort = 46337;
        private const int MaximumPort = 46997;

        public GaugeDaemonOptions()
        {
            MinPortRange = MinimumPort;
            MaxPortRange = MaximumPort;
        }

        [Category("API Options")]
        [DisplayName("MinPortRange Port Number")]
        [Description("Minimum Port range that Gauge-VisualStudio should use to communicate with Gauge's API")]
        public int MinPortRange { get; set; }

        [Category("API Options")]
        [DisplayName("MaxPortRange Port Number")]
        [Description("Maximum Port range that Gauge-VisualStudio should use to communicate with Gauge's API")]
        public int MaxPortRange { get; set; }

        public override void ResetSettings()
        {
            MinPortRange = MinimumPort;
            MaxPortRange = MaximumPort;
        }
    }
}