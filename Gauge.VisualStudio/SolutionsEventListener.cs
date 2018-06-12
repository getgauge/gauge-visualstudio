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
using Gauge.VisualStudio.Core;
using Gauge.VisualStudio.Core.Extensions;
using Gauge.VisualStudio.Core.Loggers;
using Gauge.VisualStudio.Loggers;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Gauge.VisualStudio.Model;
using Microsoft.VisualStudio.Shell;

namespace Gauge.VisualStudio
{
    public sealed class SolutionsEventListener : IVsSolutionEvents, IDisposable
    {
        private readonly GaugeDaemonOptions _gaugeDaemonOptions;
        private bool _disposed;
        private IVsSolution _solution;
        private uint _solutionCookie;

        public SolutionsEventListener(GaugeDaemonOptions gaugeDaemonOptions, IServiceProvider serviceProvider)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _gaugeDaemonOptions = gaugeDaemonOptions;
            _solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            ErrorHandler.ThrowOnFailure(_solution.AdviseSolutionEvents(this, out _solutionCookie));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            var project = pHierarchy.ToProject();
            if (!project.IsGaugeProject())
                return VSConstants.S_OK;

            var slugifiedName = project.SlugifiedName();
            if (GaugeService.Instance.ContainsApiConnectionFor(slugifiedName))
                return VSConstants.S_OK;

            try
            {
                StatusBarLogger.Log($"Initializing Gauge daemon for Project: {project.Name}");
                GaugeService.Instance.RegisterGaugeProject(project, _gaugeDaemonOptions.MinPortRange,
                    _gaugeDaemonOptions.MaxPortRange);
                StatusBarLogger.Log($"Initializing Gauge Project Cache: {project.Name}");
                ProjectFactory.Initialize(project);
            }
            catch (Exception ex)
            {
                OutputPaneLogger.Error($"Failed to start Gauge Daemon: {ex}");
                return VSConstants.S_FALSE;
            }

            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            var project = pHierarchy.ToProject();
            var slugifiedName = project.SlugifiedName();

            StatusBarLogger.Log($"Clearing Gauge Project Cache: {project.Name}");
            ProjectFactory.Delete(project.SlugifiedName());
            GaugeService.Instance.KillChildProcess(slugifiedName);
            GaugeService.Reset();
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        private void Dispose(bool disposing)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_disposed || _solution == null || _solutionCookie == 0)
                return;

            if (disposing)
            {
                _solution.UnadviseSolutionEvents(_solutionCookie);
                _solutionCookie = 0;
                _solution = null;
            }

            _disposed = true;
        }
    }
}