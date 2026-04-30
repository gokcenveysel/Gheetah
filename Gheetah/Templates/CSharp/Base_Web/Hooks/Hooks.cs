using Reqnroll;
using OpenQA.Selenium;
using {{ProjectName}}.Drivers;

namespace {{ProjectName}}.Hooks
{
    [Binding]
    public class Hooks
    {
        public static IWebDriver Driver { get; private set; }

        [BeforeScenario("@Web")]
        public void BeforeScenario()
        {
            Driver = DriverFactory.CreateDriver();
        }

        [AfterScenario("@Web")]
        public void AfterScenario()
        {
            Driver?.Quit();
            Driver?.Dispose();
        }
    }
}