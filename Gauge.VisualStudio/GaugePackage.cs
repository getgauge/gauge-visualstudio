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
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE80;
using Gauge.VisualStudio.Loggers;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Process = System.Diagnostics.Process;

namespace Gauge.VisualStudio
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(GuidList.GuidGaugeVsPackagePkgString)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class GaugePackage : Package
    {
        private Events2 _DTEEvents;
        private IVsSolution _solution;
        private readonly SolutionsEventListener _solutionsEventListener = new SolutionsEventListener();
        private uint _solutionCookie;

        protected override void Initialize()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", ToString()));
            ErrorListLogger.Initialize(this);

            DTE = (DTE) GetService(typeof (DTE));

            _solution = GetService(typeof(SVsSolution)) as IVsSolution;
            _solution.AdviseSolutionEvents(_solutionsEventListener, out _solutionCookie);

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                var menuCommandId = new CommandID(GuidList.GuidGaugeVsPackageCmdSet, (int)PkgCmdIdList.formatCommand);
                var menuItem = new OleMenuCommand(MenuItemCallback, menuCommandId);
                menuItem.BeforeQueryStatus += delegate(object sender, EventArgs args)
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
                    var transformFileInfo = new FileInfo(itemFullPath);

                    var isGaugeFile =
                        string.Compare(".spec", transformFileInfo.Extension, StringComparison.OrdinalIgnoreCase) ==
                        0;
                    if (transformFileInfo.Directory == null) return;

                    if (!isGaugeFile) return;

                    menuCommand.Visible = true;
                    menuCommand.Enabled = true;
                };
                mcs.AddCommand(menuItem);
            }
            base.Initialize();
        }

        public static DTE DTE { get; private set; }

        //TODO : Move to a separate class
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

        public static bool IsSingleProjectItemSelection(out IVsHierarchy hierarchy, out uint itemid)
        {
            hierarchy = null;
            itemid = VSConstants.VSITEMID_NIL;

            var monitorSelection = GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            var solution = GetGlobalService(typeof(SVsSolution)) as IVsSolution;
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
