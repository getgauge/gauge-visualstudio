using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Gauge.VisualStudio.Models;

namespace Gauge.VisualStudio.UI
{
    public partial class ClassPicker
    {
        private readonly IEnumerable<string> _classNames;

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
                    SelectedClass = ClassListBox.Text;
                    Close();
                    break;
            }
        }
    }
}
