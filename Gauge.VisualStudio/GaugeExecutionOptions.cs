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
using Microsoft.VisualStudio.Shell;

namespace Gauge.VisualStudio
{
    public class GaugeExecutionOptions : DialogPage
    {
        private bool _useExecutionApi = false;
        public event PropertyChangedEventHandler PropertyChanged;

        [Category("Gauge")]
        [DisplayName("Use Execution API")]
        [Description("Flag to indicate if Gauge-VisualStudio should use Gauge's Execution API")]
        public bool UseExecutionAPI
        {
            get { return _useExecutionApi; }
            set
            {
                _useExecutionApi = value;
                OnPropertyChanged("UseExecutionAPI");
            }
        }

        public virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
