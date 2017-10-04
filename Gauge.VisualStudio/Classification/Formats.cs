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
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Gauge.VisualStudio.Classification
{
    internal static class Formats
    {
        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "gauge.specification")]
        [Name("gauge.specification")]
        [DisplayName("Gauge Specification")]
        [UserVisible(true)]
        private sealed class GaugeSpecificationFormat : ClassificationFormatDefinition
        {
            public GaugeSpecificationFormat()
            {
                FontRenderingSize = 18;
                IsBold = true;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "gauge.scenario")]
        [Name("gauge.scenario")]
        [DisplayName("Gauge Scenario")]
        [UserVisible(true)]
        private sealed class GaugeScenarioFormat : ClassificationFormatDefinition
        {
            public GaugeScenarioFormat()
            {
                FontRenderingSize = 16;
                IsBold = true;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "gauge.step")]
        [Name("gauge.step")]
        [DisplayName("Gauge Step")]
        [UserVisible(true)]
        private sealed class GaugeStepFormat : ClassificationFormatDefinition
        {
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "gauge.static_param")]
        [Name("gauge.static_param")]
        [DisplayName("Gauge Static Parameter")]
        [UserVisible(true)]
        private sealed class GaugeStaticParamFormat : ClassificationFormatDefinition
        {
            public GaugeStaticParamFormat()
            {
                IsBold = true;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "gauge.dynamic_param")]
        [Name("gauge.dynamic_param")]
        [DisplayName("Gauge Dynamic Parameter")]
        [UserVisible(true)]
        private sealed class GaugeDynamicParamFormat : ClassificationFormatDefinition
        {
            public GaugeDynamicParamFormat()
            {
                IsBold = true;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "gauge.tag")]
        [Name("gauge.tag")]
        [DisplayName("Gauge Tag")]
        [UserVisible(true)]
        private sealed class GaugeTagFormat : ClassificationFormatDefinition
        {
            public GaugeTagFormat()
            {
                IsBold = true;
            }
        }

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = "gauge.tagvalue")]
        [Name("gauge.tagvalue")]
        [DisplayName("Gauge Tag Value")]
        [UserVisible(true)]
        private sealed class GaugeTagValueFormat : ClassificationFormatDefinition
        {
            public GaugeTagValueFormat()
            {
                IsBold = true;
                IsItalic = true;
            }
        }
    }
}