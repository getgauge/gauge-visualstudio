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
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Gauge.VisualStudio.Extensions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Gauge.VisualStudio
{
    public class FormatMenuCommand
    {
        private readonly IServiceProvider _serviceProvider;

        public FormatMenuCommand(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Register()
        {
            var mcs = _serviceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            var menuCommandId = new CommandID(GuidList.GuidGaugeVsPackageCmdSet, (int)PkgCmdIdList.formatCommand);
            var menuItem = new OleMenuCommand(MenuItemCallback, menuCommandId);
            menuItem.BeforeQueryStatus += MenuItemOnBeforeQueryStatus;
            mcs.AddCommand(menuItem);
        }

        private static void MenuItemOnBeforeQueryStatus(object sender, EventArgs args)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null) return;

            menuCommand.Visible = false;
            menuCommand.Enabled = false;

            string itemFullPath;

            IVsHierarchy hierarchy;
            uint itemid;

            if (!IsSingleProjectItemSelection(out hierarchy, out itemid)) return;

            ((IVsProject)hierarchy).GetMkDocument(itemid, out itemFullPath);

            if (!itemFullPath.IsGaugeSpecFile()) return;

            menuCommand.Visible = true;
            menuCommand.Enabled = true;
        }

        private static void MenuItemCallback(object sender, EventArgs e)
        {
            string itemFullPath;

            IVsHierarchy hierarchy;
            uint itemid;

            if (!IsSingleProjectItemSelection(out hierarchy, out itemid)) return;

            ((IVsProject)hierarchy).GetMkDocument(itemid, out itemFullPath);
            var gaugeFile = new FileInfo(itemFullPath);

            var arguments = string.Format(@"--simple-console --format {0}", gaugeFile.Name);
            var p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = gaugeFile.Directory.ToString(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    FileName = "gauge.exe",
                    RedirectStandardError = true,
                    Arguments = arguments,
                }
            };
            p.Start();
            p.WaitForExit();
        }

        private static bool IsSingleProjectItemSelection(out IVsHierarchy hierarchy, out uint itemid)
        {
            hierarchy = null;
            itemid = VSConstants.VSITEMID_NIL;

            var monitorSelection = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            if (monitorSelection == null || solution == null)
            {
                return false;
            }

            var hierarchyPtr = IntPtr.Zero;
            var selectionContainerPtr = IntPtr.Zero;

            try
            {
                IVsMultiItemSelect multiItemSelect = null;
                var hr = monitorSelection.GetCurrentSelection(out hierarchyPtr, out itemid, out multiItemSelect,
                    out selectionContainerPtr);

                if (ErrorHandler.Failed(hr) || hierarchyPtr == IntPtr.Zero || itemid == VSConstants.VSITEMID_NIL)
                {
                    return false;
                }

                if (multiItemSelect != null) return false;

                if (itemid == VSConstants.VSITEMID_ROOT) return false;

                hierarchy = Marshal.GetObjectForIUnknown(hierarchyPtr) as IVsHierarchy;
                if (hierarchy == null) return false;

                Guid guidProjectId;

                return !ErrorHandler.Failed(solution.GetGuidOfProject(hierarchy, out guidProjectId));
            }
            finally
            {
                if (selectionContainerPtr != IntPtr.Zero)
                {
                    Marshal.Release(selectionContainerPtr);
                }

                if (hierarchyPtr != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPtr);
                }
            }
        }
    }
}