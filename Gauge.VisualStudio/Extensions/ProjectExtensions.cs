using EnvDTE;

namespace Gauge.VisualStudio.Extensions
{
    internal static class ProjectExtensions
    {
        public static string SlugifiedName(this Project project)
        {
            return project.Name.Replace('.', '_');
        }
    }
}
