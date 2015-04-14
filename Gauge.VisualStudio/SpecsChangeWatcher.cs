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