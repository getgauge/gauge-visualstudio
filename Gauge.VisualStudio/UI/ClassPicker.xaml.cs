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

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Gauge.VisualStudio.Models;
using Microsoft.CSharp;
using Color = System.Windows.Media.Color;

namespace Gauge.VisualStudio.UI
{
    public partial class ClassPicker
    {
        private readonly IEnumerable<string> _classNames;
        private readonly CSharpCodeProvider _cSharpCodeProvider = new CSharpCodeProvider();
        private readonly SolidColorBrush _redColor = new SolidColorBrush(Color.FromRgb(255,0,0));
        private readonly SolidColorBrush _blackColor = new SolidColorBrush(Color.FromRgb(0,0,0));

        public string SelectedClass { get; private set; }

        public ClassPicker()
        {
            InitializeComponent();
            WindowStartupLocation=WindowStartupLocation.CenterScreen;
            _classNames = Project.GetAllClasses().Select(element => element.Name).Take(10);
            ClassListBox.ItemsSource = _classNames;
        }

        private void ClassPicker_OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    break;
                case Key.Enter:
                    if (IsValidIdentifier())
                    {
                        SelectedClass = ClassListBox.Text;
                        Close();
                    }
                    break;
            }
            ClassListBox.Foreground = IsValidIdentifier() ? _blackColor : _redColor;
        }

        private bool IsValidIdentifier()
        {
            return _cSharpCodeProvider.IsValidIdentifier(ClassListBox.Text);
        }
    }
}
