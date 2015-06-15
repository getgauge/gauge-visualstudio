using System;
using Microsoft.VisualStudio.Shell;

namespace Gauge.VisualStudio
{
    internal static class ErrorListLogger
    {
        private static ErrorListProvider _errorListProvider;

        public static void Initialize(IServiceProvider serviceProvider)
        {
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
