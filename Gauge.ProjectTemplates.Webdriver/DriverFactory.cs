using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.IE;

namespace $safeprojectname$
{
    public class DriverFactory
    {
        public static IWebDriver GetDriver()
        {
            var browser = Environment.GetEnvironmentVariable("BROWSER");
            switch (browser)
            {
                case "chrome":
                    return new ChromeDriver();
                case "ie":
                    return new InternetExplorerDriver();
                default:
                    return new FirefoxDriver();
            }
        }
    }
}
