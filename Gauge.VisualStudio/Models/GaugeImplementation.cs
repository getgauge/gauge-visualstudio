using EnvDTE;

namespace Gauge.VisualStudio.Models
{
    internal class GaugeImplementation
    {
        public CodeFunction Function { get; set; }
        public dynamic StepAttribute { get; set; }

        public bool ContainsFor(string givenText)
        {
            foreach (var arg in StepAttribute.Arguments)
            {
                string input = arg.Value.ToString().Trim('"');

                if (Step.GetStepValueFromInput(input) == Step.GetStepValueFromInput(givenText))
                    return true;
            }
            return false;
        }
    }
}