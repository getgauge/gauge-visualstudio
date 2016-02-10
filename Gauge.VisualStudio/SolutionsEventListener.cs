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
using Gauge.VisualStudio.Core.Extensions;
using Gauge.VisualStudio.Core.Helpers;
using Gauge.VisualStudio.Loggers;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Gauge.VisualStudio
{
    public sealed class SolutionsEventListener : IVsSolutionEvents, IDisposable    {
        private IVsSolution _solution;
        private uint _solutionCookie;
        private bool _disposed;

        public SolutionsEventListener()
        {
            _solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
            ErrorHandler.ThrowOnFailure(_solution.AdviseSolutionEvents(this, out _solutionCookie));
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            var project = pHierarchy.ToProject();
            if (!project.IsGaugeProject())
                return VSConstants.S_OK;

            var slugifiedName = project.SlugifiedName();
            if (GaugeDaemonHelper.ContainsApiConnectionFor(slugifiedName))
                return VSConstants.S_OK;

            GaugeDaemonHelper.RegisterGaugeProject(project);

            StatusBarLogger.Log("Gauge Project detected, build solution to keep test explorer updated.");
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

            GaugeDaemonHelper.KillChildProcess(slugifiedName);

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

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
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