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
using System.IO;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Gauge.VisualStudio.Loggers
{
    class StatusBarLogger
    {
        internal static void Log(string message)
        {
            var statusBar = GaugePackage.DTE.StatusBar;
            statusBar.Text = message;
            statusBar.Highlight(true);
            Console.WriteLine(message);
        }
    }

    class OutputPaneLogger
    {
        internal static void WriteLine(string message, params object[] parameters)
        {
            Write(string.Concat(message, "\n"), parameters);
        }

        internal static void Write(string message, params object[] parameters)
        {
            var outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

            var customGuid = new Guid("82304FBB-CBBC-4F53-AE58-DD58F0B46A54");
            const string customTitle = "Gauge";
            if (outWindow == null)
                return;

            outWindow.CreatePane(ref customGuid, customTitle, 1, 1);

            IVsOutputWindowPane customPane;
            outWindow.GetPane(ref customGuid, out customPane);

            customPane.OutputString(string.Format(message, parameters));
        }
    }
}
