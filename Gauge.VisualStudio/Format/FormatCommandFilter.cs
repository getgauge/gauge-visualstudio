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
using System.IO;
using Gauge.VisualStudio.Core;
using Gauge.VisualStudio.Core.Loggers;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;

namespace Gauge.VisualStudio.Format
{
    internal class FormatCommandFilter : IOleCommandTarget
    {
        public FormatCommandFilter(IWpfTextView textView)
        {
            TextView = textView;
        }

        private IWpfTextView TextView { get; }
        public IOleCommandTarget Next { get; set; }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if ((VSConstants.VSStd2KCmdID)prgCmds[0].cmdID != VSConstants.VSStd2KCmdID.FORMATDOCUMENT)
                return Next.QueryStatus(pguidCmdGroup, cCmds, prgCmds, pCmdText);

            prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_ENABLED | (uint)OLECMDF.OLECMDF_SUPPORTED;
            return VSConstants.S_OK;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if ((VSConstants.VSStd2KCmdID)nCmdID != VSConstants.VSStd2KCmdID.FORMATDOCUMENT)
            {
                return Next.Exec(pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
            }

            var gaugeFile = new FileInfo(GaugePackage.DTE.ActiveDocument.FullName);

            var p = GaugeProcess.ForFormat(gaugeFile.DirectoryName, gaugeFile.Name);
            p.Start();
            p.WaitForExit();
            if (p.ExitCode != 0)
            {
                OutputPaneLogger.Error($"gauge format {gaugeFile.Name}\nSTDOUT:\n{p.StandardOutput.ReadToEnd()}\nSTDERR:\n{p.StandardError.ReadToEnd()}\n");
            }
            return VSConstants.S_OK;
        }
    }
}
