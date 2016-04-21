using System;
using FluentAssertions;
using Gauge.CSharp.Lib.Attribute;
using OpenQA.Selenium;

namespace $safeprojectname$
{
    public class StepImplementation
    {
        private IWebDriver _driver;

        [BeforeSuite]
        public void Initialize()
        {
            _driver = DriverFactory.GetDriver();
        }

        [AfterSuite]
        public void Cleanup()
        {
            _driver.Quit();
        }

        [Step("Navigate to Gauge homepage <url>")]
        public void NavigateTo(string url)
        {
            _driver.Navigate().GoToUrl(url);

            _driver.Title.Should().Be("Gauge | ThoughtWorks");
        }

        [Step("Go to Gauge Get Started Page")]
        public void GoToGaugeGetStartedPage()
        {
            var getStartedButton = _driver.FindElement(By.LinkText("Get Started"));
            getStartedButton.Click();
        }
    }
}