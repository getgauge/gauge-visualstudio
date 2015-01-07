using System;
using System.IO;
using Gauge.VisualStudio.Models;

namespace Gauge.VisualStudio
{
    public class SpecsChangeWatcher : IDisposable
    {
        private readonly FileSystemWatcher _watcher = new FileSystemWatcher();

        private readonly FileSystemWatcher _implementationWatcher = new FileSystemWatcher();

        internal void Watch(string basePath)
        {
            _watcher.Path = Path.Combine(basePath, "specs");
            _watcher.IncludeSubdirectories = true;
            _watcher.EnableRaisingEvents = true;
            _watcher.Changed += WatcherOnChanged;
            _watcher.Created += WatcherOnChanged;
            _watcher.Renamed += WatcherOnChanged;
            _watcher.Deleted += WatcherOnChanged;
            _watcher.Filter = "*.*";
            _implementationWatcher.Path = basePath;
            _implementationWatcher.IncludeSubdirectories = true;
            _implementationWatcher.EnableRaisingEvents = true;
            _implementationWatcher.Changed += WatcherOnChanged;
            _implementationWatcher.Created += WatcherOnChanged;
            _implementationWatcher.Renamed += WatcherOnChanged;
            _implementationWatcher.Deleted += WatcherOnChanged;
            _implementationWatcher.Filter = "*.cs";
        }

        private static void WatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            Concept.Refresh();
            Step.Refresh();
        }

        public void Dispose()
        {
            _watcher.Changed -= WatcherOnChanged;
            _watcher.Created -= WatcherOnChanged;
            _watcher.Deleted -= WatcherOnChanged;
            _watcher.Renamed -= WatcherOnChanged;
            _watcher.Dispose();
            _implementationWatcher.Changed -= WatcherOnChanged;
            _implementationWatcher.Deleted -= WatcherOnChanged;
            _implementationWatcher.Created -= WatcherOnChanged;
            _implementationWatcher.Renamed -= WatcherOnChanged;
            _implementationWatcher.Dispose();
        }
    }
}