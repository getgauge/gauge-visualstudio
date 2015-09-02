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

using System.ComponentModel.Composition;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestWindow.Extensibility;

namespace Gauge.VisualStudio.TestRunner
{
    [Export(typeof(ISettingsProvider))]
    [Export(typeof(IRunSettingsService))]
    [SettingsName(GaugeTestRunSettings.SettingsName)]
    public class GaugeTestRunSettingsService : IRunSettingsService, ISettingsProvider
    {
        public GaugeTestRunSettingsService()
        {
            Name = GaugeTestRunSettings.SettingsName;
            Settings = new GaugeTestRunSettings();
            Serializer = new XmlSerializer(typeof (GaugeTestRunSettings));
        }

        public XmlSerializer Serializer { get; private set; }

        public GaugeTestRunSettings Settings { get; private set; }

        public IXPathNavigable AddRunSettings(IXPathNavigable inputRunSettingDocument, IRunSettingsConfigurationInfo configurationInfo,
            ILogger log)
        {
            ValidateArg.NotNull(inputRunSettingDocument, "inputRunSettingDocument");
            ValidateArg.NotNull(configurationInfo, "configurationInfo");

            var navigator = inputRunSettingDocument.CreateNavigator();

            if (navigator.MoveToChild("RunSettings", ""))
            {
                if (navigator.MoveToChild(GaugeTestRunSettings.SettingsName, ""))
                {
                    navigator.DeleteSelf();
                }

                navigator.AppendChild(SerializeGaugeSettings());
            }

            navigator.MoveToRoot();
            return navigator;
        }

        public string Name { get; private set; }

        public void Load(XmlReader reader)
        {
            ValidateArg.NotNull(reader, "reader");

            if (reader.Read() && reader.Name.Equals(GaugeTestRunSettings.SettingsName))
            {
                Settings = Serializer.Deserialize(reader) as GaugeTestRunSettings;
            }
        }

        private string SerializeGaugeSettings()
        {
            var stringWriter = new StringWriter();
            Serializer.Serialize(stringWriter, Settings);
            return stringWriter.ToString();
        }
    }
}