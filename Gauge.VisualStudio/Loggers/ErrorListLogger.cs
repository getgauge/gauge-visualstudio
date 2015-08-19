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
using Microsoft.VisualStudio.Shell;

namespace Gauge.VisualStudio.Loggers
{
    internal static class ErrorListLogger
    {
        private static ErrorListProvider _errorListProvider;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            if (_errorListProvider != null) return;
            Console.WriteLine("*** Initialized error list provider");
            _errorListProvider = new ErrorListProvider(serviceProvider);
        }

        public static void AddError(string message)
        {
            AddTask(message, TaskErrorCategory.Error);
        }

        public static void AddWarning(string message)
        {
            AddTask(message, TaskErrorCategory.Warning);
        }

        public static void AddMessage(string message)
        {
            AddTask(message, TaskErrorCategory.Message);
        }

        private static void AddTask(string message, TaskErrorCategory category)
        {
            _errorListProvider.Tasks.Add(new ErrorTask
            {
                Category = TaskCategory.User,
                ErrorCategory = category,
                Text = message
            });
        }
    }
}
