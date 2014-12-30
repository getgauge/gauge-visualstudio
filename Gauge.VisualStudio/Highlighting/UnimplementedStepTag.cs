using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Gauge.VisualStudio.Highlighting
{
    internal class UnimplementedStepTag : SmartTag
    {
        public UnimplementedStepTag(SmartTagType smartTagType, ReadOnlyCollection<SmartTagActionSet> actionSets) : base(smartTagType, actionSets)
        {
        }
    }
}