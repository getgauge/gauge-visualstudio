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

using System.Windows;

namespace Gauge.VisualStudio.UI
{
    public partial class RefactorDialog
    {
        public string StepText { get; private set; }

        public RefactorDialog(string stepText)
        {
            InitializeComponent();
            StepTextBox.Text = stepText;
            StepTextBox.Focus();
            StepTextBox.SelectAll();
            WindowStartupLocation=WindowStartupLocation.CenterScreen;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            StepText=StepTextBox.Text;
            DialogResult = true;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
