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
using Gauge.Messages;
using Gauge.VisualStudio.Core;
using Gauge.VisualStudio.Core.Exceptions;

namespace Gauge.VisualStudio.Model
{
    public class Specification
    {
        public static IEnumerable<string> GetAllSpecsFromGauge()
        {
            try
            {
                GaugeService.Instance.AssertCompatibility();
            }
            catch (GaugeVersionIncompatibleException ex)
            {
                GaugeService.Instance.DisplayGaugeNotStartedMessage(
                    "Unable to launch Gauge Daemon. Check Output Window for details", ex.Data["GaugeError"].ToString(),
                    GaugeDisplayErrorLevel.Error);
                return Enumerable.Empty<string>();
            }
            catch (GaugeVersionNotFoundException ex)
            {
                GaugeService.Instance.DisplayGaugeNotStartedMessage(
                    "Unable to launch Gauge Daemon. Check Output Window for details", ex.Data["GaugeError"].ToString(),
                    GaugeDisplayErrorLevel.Error);
                return Enumerable.Empty<string>();
            }

            var specifications = new List<ProtoSpec>();
            var gaugeServiceClient = new GaugeServiceClient();
            try
            {
                foreach (var apiConnection in GaugeService.Instance.GetAllApiConnections())
                    specifications.AddRange(gaugeServiceClient.GetSpecsFromGauge(apiConnection));

                return specifications.Select(spec => spec.FileName).Distinct();
            }
            catch (GaugeApiInitializationException)
            {
                return Enumerable.Empty<string>();
            }
        }

        public static IEnumerable<ProtoSpec> GetAllSpecs(int apiPort)
        {
            return new GaugeServiceClient().GetSpecsFromGauge(apiPort);
        }
    }
}