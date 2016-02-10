using Gauge.CSharp.Core;
using Gauge.VisualStudio.Core.Helpers;

namespace Gauge.VisualStudio.Helpers
{
    internal class DaemonHelper
    {
        public static GaugeApiConnection GetApiConnectionForActiveDocument()
        {
            return GaugeDaemonHelper.GetApiConnectionFor(GaugePackage.ActiveProject);
        }
    }
}