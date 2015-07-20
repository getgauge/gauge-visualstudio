using System.Collections.Generic;
using System.Runtime.CompilerServices;
using main;

namespace Gauge.VisualStudio.TestRunner
{
    public static class SpecsHolder
    {
        public static List<ProtoSpec> _specs = new List<ProtoSpec>(); 
        public static List<ProtoSpec> Specs {
            get { return _specs; }
            set
            {
                _specs = value;
            }
        }

    }
}