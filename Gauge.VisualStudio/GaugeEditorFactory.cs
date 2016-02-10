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
using System.Runtime.InteropServices;
using Gauge.VisualStudio.Core.Extensions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Gauge.VisualStudio
{
    [Guid(GuidList.GuidGaugeEditorFactorString)]
    public class GaugeEditorFactory : IVsEditorFactory
    {
        private readonly Package _package;
        private IServiceProvider _serviceProvider;

        public GaugeEditorFactory(Package package)
        {
            _package = package;
        }

        public int CreateEditorInstance(uint grfCreateDoc, string pszMkDocument, string pszPhysicalView, IVsHierarchy pvHier,
            uint itemid, IntPtr punkDocDataExisting, out IntPtr ppunkDocView, out IntPtr ppunkDocData,
            out string pbstrEditorCaption, out Guid pguidCmdUI, out int pgrfCDW)
        {
            ppunkDocView = IntPtr.Zero;
            ppunkDocData = IntPtr.Zero;
            pbstrEditorCaption = string.Empty;
            pguidCmdUI = Guid.Empty;
            pgrfCDW = 0;

            var isGaugeProject = pvHier.ToProject().IsGaugeProject();

            if (!isGaugeProject)
            {
                return VSConstants.VS_E_UNSUPPORTEDFORMAT;
            }

            if ((grfCreateDoc & (VSConstants.CEF_OPENFILE | VSConstants.CEF_SILENT)) == 0)
            {
                throw new ArgumentException("Only Open or Silent is valid");
            }

            var pTextBuffer = GetTextBuffer(punkDocDataExisting);
            if (pTextBuffer == null)
            {
                return VSConstants.E_FAIL;
            }

            var textBufferSite = pTextBuffer as IObjectWithSite;
            if (textBufferSite != null)
            {
                textBufferSite.SetSite(_serviceProvider);
            }

            var clsidCodeWindow = typeof(VsCodeWindowClass).GUID;
            var iidCodeWindow = typeof(IVsCodeWindow).GUID;
            var pCodeWindow = (IVsCodeWindow)_package.CreateInstance(ref clsidCodeWindow, ref iidCodeWindow, typeof(IVsCodeWindow));

            if (pCodeWindow == null)
            {
                return VSConstants.E_FAIL;
            }

            var bufferEventListener = new TextBufferEventListener(pTextBuffer);
            pCodeWindow.SetBuffer(pTextBuffer);

            if (!(punkDocDataExisting == IntPtr.Zero))
            {
                bufferEventListener.OnLoadCompleted(0);
            }

            ppunkDocView = Marshal.GetIUnknownForObject(pCodeWindow);
            ppunkDocData = Marshal.GetIUnknownForObject(pTextBuffer);
            pguidCmdUI = VSConstants.GUID_TextEditorFactory;
            pbstrEditorCaption = string.Empty;
            return VSConstants.S_OK;
        }

        private IVsTextLines GetTextBuffer(IntPtr docDataExisting)
        {
            IVsTextLines textLines;
            if (docDataExisting == IntPtr.Zero)
            {
                var textLinesType = typeof(IVsTextLines);
                var riid = textLinesType.GUID;
                var clsid = typeof(VsTextBufferClass).GUID;
                textLines = _package.CreateInstance(ref clsid, ref riid, textLinesType) as IVsTextLines;
                ((IObjectWithSite)textLines).SetSite(_serviceProvider);
            }
            else
            {
                var dataObject = Marshal.GetObjectForIUnknown(docDataExisting);
                textLines = dataObject as IVsTextLines;
                if (textLines == null)
                {
                    var textBufferProvider = dataObject as IVsTextBufferProvider;
                    if (textBufferProvider != null)
                    {
                        textBufferProvider.GetTextBuffer(out textLines);
                    }
                }
                if (textLines == null)
                {
                    throw Marshal.GetExceptionForHR(VSConstants.VS_E_INCOMPATIBLEDOCDATA);
                }
            }
            return textLines;
        }


        public int SetSite(IServiceProvider psp)
        {
            _serviceProvider = psp;
            return VSConstants.S_OK;
        }

        public int Close()
        {
            return VSConstants.S_OK;
        }

        public int MapLogicalView(ref Guid rguidLogicalView, out string pbstrPhysicalView)
        {
            pbstrPhysicalView = null;
            if (rguidLogicalView.Equals(VSConstants.LOGVIEWID_Designer) ||
                rguidLogicalView.Equals(VSConstants.LOGVIEWID_Primary))
            {
                return VSConstants.S_OK;
            }

            return VSConstants.E_NOTIMPL;
        }

        public class TextBufferEventListener : IVsTextBufferDataEvents
        {
            private readonly IVsTextLines _textLines;
            private readonly IConnectionPoint _connectionPoint;
            private readonly uint _cookie;

            public TextBufferEventListener(IVsTextLines textLines)
            {
                _textLines = textLines;
                var connectionPointContainer = textLines as IConnectionPointContainer;
                var bufferEventsGuid = typeof(IVsTextBufferDataEvents).GUID;
                connectionPointContainer.FindConnectionPoint(ref bufferEventsGuid, out _connectionPoint);
                _connectionPoint.Advise(this, out _cookie);
            }

            public void OnFileChanged(uint grfChange, uint dwFileAttrs)
            {
            }

            public int OnLoadCompleted(int fReload)
            {
                _connectionPoint.Unadvise(_cookie);
                
                var languageServiceId = typeof(GaugeLanguageInfo).GUID;
                _textLines.SetLanguageServiceID(ref languageServiceId);

                return VSConstants.S_OK;
            }
        }
    }
}