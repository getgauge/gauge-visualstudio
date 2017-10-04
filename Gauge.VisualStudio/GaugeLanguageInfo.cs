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

using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Gauge.VisualStudio
{
    [Guid(GuidList.GuidGaugeLanguageInfoString)]
    internal class GaugeLanguageInfo : IVsLanguageInfo
    {
        internal const string LanguageName = GaugeContentTypeDefinitions.GaugeContentType;
        public const int LanguageResourceId = 112;

        private static readonly string[] FileExtensions =
        {
            GaugeContentTypeDefinitions.ConceptFileExtension,
            GaugeContentTypeDefinitions.MarkdownFileExtension,
            GaugeContentTypeDefinitions.SpecFileExtension
        };

        private readonly SVsServiceProvider _serviceProvider;

        public int GetLanguageName(out string bstrName)
        {
            bstrName = LanguageName;
            return VSConstants.S_OK;
        }

        public int GetFileExtensions(out string pbstrExtensions)
        {
            pbstrExtensions = string.Join(";", FileExtensions);
            return VSConstants.S_OK;
        }

        public int GetColorizer(IVsTextLines pBuffer, out IVsColorizer ppColorizer)
        {
            ppColorizer = null;
            return VSConstants.E_FAIL;
        }

        public int GetCodeWindowManager(IVsCodeWindow pCodeWin, out IVsCodeWindowManager ppCodeWinMgr)
        {
            ppCodeWinMgr = null;
            return VSConstants.E_FAIL;
        }
    }
}