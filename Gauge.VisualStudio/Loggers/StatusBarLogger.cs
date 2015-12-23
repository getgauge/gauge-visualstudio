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
        internal static void Write(string message)
        {
            var outWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

            var customGuid = new Guid("82304FBB-CBBC-4F53-AE58-DD58F0B46A54");
            const string customTitle = "Gauge";
            if (outWindow == null)
                return;

            outWindow.CreatePane(ref customGuid, customTitle, 1, 1);

            IVsOutputWindowPane customPane;
            outWindow.GetPane(ref customGuid, out customPane);

            customPane.OutputString(message);
        }
    }
}
