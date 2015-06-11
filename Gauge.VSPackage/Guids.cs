// Guids.cs
// MUST match guids.h
using System;

namespace Company.Gauge_VSPackage
{
    static class GuidList
    {
        public const string guidGauge_VSPackagePkgString = "309aa1cd-4dc1-43e9-9d19-85b21abf2520";
        public const string guidGauge_VSPackageCmdSetString = "d09143fc-0d23-4d55-9fda-4d90e5da0c3f";

        public static readonly Guid guidGauge_VSPackageCmdSet = new Guid(guidGauge_VSPackageCmdSetString);
    };
}