using System.Linq;
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
        public void ShouldGiveSpecHeadingOnly()
        {
            var specName = Parser.GetSpecificationName("Spec Heading\n==============");
            Assert.AreEqual("Spec Heading", specName);
        }
        
        [Test]
        public void ShouldGiveSpecHeadingThatIsTrimmed()
        {
            var specName = Parser.GetSpecificationName("Spec Heading  \n==============");
            Assert.AreEqual("Spec Heading", specName);
            
            specName = Parser.GetSpecificationName("Spec Heading  \n  ==============");
            Assert.AreEqual("Spec Heading", specName);

            specName = Parser.GetSpecificationName("  Spec Heading  \n     ==============");
            Assert.AreEqual("Spec Heading", specName);

            specName = Parser.GetSpecificationName("  Spec Heading  \n ");
            Assert.AreEqual(string.Empty, specName);

            specName = Parser.GetSpecificationName("  Spec Heading  \n = ");
            Assert.AreEqual("Spec Heading", specName);

            specName = Parser.GetSpecificationName("  Spec Heading  ==\n = ");
            Assert.AreEqual("Spec Heading  ==", specName);
        }

        [Test]
        public void ShouldGiveSpecHeadingThatHasSpecialCharacters()
        {
            var specName = Parser.GetSpecificationName("  Spec Heading 123!@$%^&*(){};,.?/|[]==\n = hello all");
            Assert.AreEqual("Spec Heading 123!@$%^&*(){};,.?/|[]==", specName);

            specName = Parser.GetSpecificationName("  456!@Spec Heading 123!@$%^&*(){};,.?/|[]==\n = hello all");
            Assert.AreEqual("456!@Spec Heading 123!@$%^&*(){};,.?/|[]==", specName);
        }

        [Test]
        public void ShouldGiveSpecHeadingWrittenInMarkdownFormat()
        {
            var specName = Parser.GetSpecificationName("#Spec Heading\n New Scenario");
            Assert.AreEqual("Spec Heading", specName);

            specName = Parser.GetSpecificationName("#Spec Heading");
            Assert.AreEqual("Spec Heading", specName);

            specName = Parser.GetSpecificationName("#Customer Specification \n ##scenario");
            Assert.AreEqual("Customer Specification", specName);

            specName = Parser.GetSpecificationName("#Spec Heading \n\n ##Scenario Heading \n  * Say \"hello\" to \"gauge\" \n \n ");
            Assert.AreEqual("Spec Heading", specName);

            specName = Parser.GetSpecificationName("#My Markdown Spec Heading\n\n##My Markdown scenario Heading\n\n* Say \"hello\" to \"gauge\"");
            Assert.AreEqual("My Markdown Spec Heading", specName);

        }


        [Test]
        public void ShouldGiveScenarioHeading()
        {
            var spec = "Spec heading \n ========== \n Scenario Heading \n---------------";
            var scenarioNames = Parser.GetScenarios(spec);
            Assert.AreEqual("Scenario Heading", scenarioNames.First());
            Assert.AreEqual("Spec heading", Parser.GetSpecificationName(spec));
            
            scenarioNames = Parser.GetScenarios("Spec heading \n ========== \n Scenario Heading \n ---------------");
            Assert.AreEqual("Scenario Heading", scenarioNames.First());
        }

        [Test]
        public void ShouldGiveScenarioHeadingWrittenInMarkdown()
        {
            var scenarioNames = Parser.GetScenarios("#Spec heading \n \n ##Scenario Heading \n");
            Assert.AreEqual("Scenario Heading", scenarioNames.First());

            scenarioNames = Parser.GetScenarios("#Spec heading \n##Scenario Heading \n");
            Assert.AreEqual("Scenario Heading", scenarioNames.First());

            scenarioNames = Parser.GetScenarios("#Spec heading\n##Scenario Heading\n##Second Scenario\n");
            var enumerable = scenarioNames as string[] ?? scenarioNames.ToArray();
            Assert.AreEqual("Scenario Heading", enumerable[0]);
            Assert.AreEqual("Second Scenario", enumerable[1]);
            
            scenarioNames = Parser.GetScenarios("#Spec heading\n\n ##Scenario Heading\n * Say hi to all \n ##Second Scenario\n");
            enumerable = scenarioNames as string[] ?? scenarioNames.ToArray();
            Assert.AreEqual("Scenario Heading", enumerable[0]);
            Assert.AreEqual("Second Scenario", enumerable[1]);

            
        }

        [Test]
        public void ShouldGiveMultipleScenarioHeadings()
        {
            var spec = "Spec Heading \n ==================\n Scenario Heading \n --------------\n * Say \"hello\" to \"gauge\" \n \n " +
                          "Second Scenario Heading \n --------------\n This is second scenario" +
                          "\n Third Scenario\n--------------\n This is third scenario";
            var scenarioNames = Parser.GetScenarios(spec);
            var enumerable = scenarioNames as string[] ?? scenarioNames.ToArray();
            Assert.AreEqual(3, enumerable.Count());
            Assert.AreEqual("Scenario Heading", enumerable[0]);
            Assert.AreEqual("Second Scenario Heading", enumerable[1]);
            Assert.AreEqual("Third Scenario", enumerable[2]);
            Assert.AreEqual("Spec Heading", Parser.GetSpecificationName(spec));
        }
        
        [Test]
        public void ShouldGiveMultipleScenarioHeadingsWrittenInMarkdownFormat()
        {
            var spec = "#Spec Heading \n\n ##Scenario Heading \n  * Say \"hello\" to \"gauge\" \n \n " +
                          "##Second Scenario Heading \n This is second scenario" +
                          "\n Third Scenario\n--------------\n This is third scenario";
            var scenarioNames = Parser.GetScenarios(spec);
            var enumerable = scenarioNames as string[] ?? scenarioNames.ToArray();
            Assert.AreEqual(3, enumerable.Count());
            Assert.AreEqual("Scenario Heading", enumerable[0]);
            Assert.AreEqual("Second Scenario Heading", enumerable[1]);
            Assert.AreEqual("Third Scenario", enumerable[2]);
            Assert.AreEqual("Spec Heading", Parser.GetSpecificationName(spec));
        }

    }

}