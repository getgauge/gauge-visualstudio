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
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Gauge.VisualStudio.Core;
using Gauge.VisualStudio.Core.Helpers;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Gauge.VisualStudio.TestAdapter
{
    public class GaugeTestRunSettings : TestRunSettings
    {
        public const string SettingsName = "GaugeTestRunSettings";

        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(GaugeTestRunSettings));

        public GaugeTestRunSettings(string name) : base(name)
        {
            ProjectsProperties = GaugeService.Instance.GetPropertiesForAllGaugeProjects();
        }

        public GaugeTestRunSettings() : this(SettingsName)
        {
        }

        public bool UseExecutionAPI { get; set; }

        public List<GaugeProjectProperties> ProjectsProperties { get; }

        public override XmlElement ToXml()
        {
            var stringWriter = new StringWriter();
            Serializer.Serialize(stringWriter, this);
            var document = new XmlDocument();
            document.LoadXml(stringWriter.ToString());
            return document.DocumentElement;
        }
    }
}