using System;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Company.Gauge_VSPackage
{
    class SolutionEventsListener : IVsSolutionEvents
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IVsSolution _solution;
        private uint _cookie;

        public SolutionEventsListener(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
        }

        public void StartListeningForChanges()
        {
            if (_solution == null) return;

            var hr = _solution.AdviseSolutionEvents(this, out _cookie);
            ErrorHandler.ThrowOnFailure(hr);
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            if (_solution == null) return VSConstants.S_OK;

            var dte = _serviceProvider.GetService(typeof(DTE)) as DTE;
            if (dte != null && dte.ActiveDocument !=null && new [] {".spec", ".cpt"}.Any(dte.ActiveDocument.Name.Contains))
            {
                dte.ActiveDocument.Save();
            }
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
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
    }
}
