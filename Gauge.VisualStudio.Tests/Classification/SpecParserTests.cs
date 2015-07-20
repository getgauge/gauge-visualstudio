using System;
using Gauge.VisualStudio.Classification;
using NUnit.Framework;

namespace Gauge.VisualStudio.Tests.Classification
{

    [TestFixture]
    public class SpecParserTests
    {
        [Test]
        public void ShouldGiveSpecHeadingAsEmptyWhenEmptyStringIsPassed()
        {
            var specName = Parser.GetSpecificationName("");
            Assert.AreEqual("", specName);
        }

        [Test]
        public void ShouldGiveSpecHeading()
        {
            var specName = Parser.GetSpecificationName("Spec Heading\\n ==============");
            Assert.AreEqual("Spec Heading", specName);
        }
    }

}