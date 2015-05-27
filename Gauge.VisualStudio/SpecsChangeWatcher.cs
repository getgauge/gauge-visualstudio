using System;
using System.IO;
using Gauge.VisualStudio.Models;
using Project = EnvDTE.Project;

namespace Gauge.VisualStudio
{
    public class SpecsChangeWatcher : IDisposable
    {
        private readonly Project _gaugeProject;
        private readonly FileSystemWatcher _watcher = new FileSystemWatcher();

        private readonly FileSystemWatcher _implementationWatcher = new FileSystemWatcher();

        public SpecsChangeWatcher(Project gaugeProject)
        {
            _gaugeProject = gaugeProject;
            var basePath = Path.GetDirectoryName(_gaugeProject.FullName);
            _watcher.Path = basePath;
            _watcher.IncludeSubdirectories = true;
            _watcher.NotifyFilter = NotifyFilters.LastWrite;
            _watcher.EnableRaisingEvents = true;
            _watcher.Changed += WatcherOnChanged;
            _watcher.Filter = "*.*";
            _implementationWatcher.Path = basePath;
            _implementationWatcher.IncludeSubdirectories = true;
            _implementationWatcher.NotifyFilter = NotifyFilters.LastWrite;
            _implementationWatcher.EnableRaisingEvents = true;
            _implementationWatcher.Changed += WatcherOnChanged;
            _implementationWatcher.Filter = "*.cs";
        }

        private void WatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            Concept.Refresh(_gaugeProject);
            Step.Refresh(_gaugeProject);
        }

        public void Dispose()
        {
            _watcher.Changed -= WatcherOnChanged;
            _watcher.Dispose();
            _implementationWatcher.Changed -= WatcherOnChanged;
            _implementationWatcher.Dispose();
        }
    }
}