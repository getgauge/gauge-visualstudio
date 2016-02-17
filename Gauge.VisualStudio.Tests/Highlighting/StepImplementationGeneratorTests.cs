using Gauge.VisualStudio.Highlighting;
using NUnit.Framework;

namespace Gauge.VisualStudio.Tests.Highlighting
{
    [TestFixture]
    public class StepImplementationGeneratorTests
    {
        [Test]
        public void ShouldGenerateImplementationWithSignature()
        {
            const string StepText = "Do nothing";
            var stepImplementationGenerator = new StepImplementationGenerator();

//            stepImplementationGenerator.TryGenerateMethodStub("foo", )
        }
    }
}
