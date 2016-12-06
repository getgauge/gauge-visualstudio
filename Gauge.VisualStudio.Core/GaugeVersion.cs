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
using System.Text.RegularExpressions;

namespace Gauge.VisualStudio.Core
{
    public class GaugeVersion : IComparable<GaugeVersion>
    {
        private readonly Regex _versionRegex = new Regex(@"^(?<Major>\d+).(?<Minor>\d+).(?<Patch>\d+)(.(?<Nightly>nightly)-)?((?<Date>\d{4}-\d{2}-\d{2}))?$", RegexOptions.Compiled);

        public GaugeVersion(string version)
        {
            var match = _versionRegex.Match(version);
            if (!match.Success)
            {
                throw new ArgumentException(string.Format("Invalid version specified : '{0}'", version));
            }

            Major = int.Parse(match.Groups["Major"].Value);
            Minor = int.Parse(match.Groups["Minor"].Value);
            Patch = int.Parse(match.Groups["Patch"].Value);

            IsNightly = match.Groups["Nightly"].Success;

            var dateMatch = match.Groups["Date"];
            if (dateMatch.Success)
            {
                Date = DateTime.Parse(dateMatch.Value);                
            }
        }

        public int Major { get; private set; }

        public int Minor { get; private set; }

        public int Patch { get; private set; }

        public bool IsNightly { get; private set; }

        public DateTime Date { get; private set; }

        public override string ToString()
        {
            if (IsNightly)
            {
                return string.Format("{0}.{1}.{2}.nightly-{3:yyyy-MM-dd}", Major, Minor, Patch, Date);
            }
            return string.Format("{0}.{1}.{2}", Major, Minor, Patch);
        }

        public int CompareTo(GaugeVersion other)
        {
            var versionCompareResult = new Version(Major, Minor, Patch).CompareTo(new Version(other.Major, other.Minor, other.Patch));

            if (versionCompareResult == 0 && IsNightly && other.IsNightly)
            {
                return Date.CompareTo(other.Date);
            }
            return versionCompareResult;
        }
    }
}