using BddAutomationProject.Drivers;
using OpenQA.Selenium;
using Reqnroll;
using Reqnroll.BoDi;

namespace BddAutomationProject.Hooks
{
    [Binding]
    public class TestSetup
    {
        private readonly IObjectContainer _objectContainer;
        private IWebDriver _driver;

        public TestSetup(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        [BeforeScenario]
        public void BeforeScenario()
        {
            var driverFactory = new WebDriverFactory();
            _driver = driverFactory.GetDriver();
            _objectContainer.RegisterInstanceAs<WebDriverFactory>(driverFactory);
            _driver.Manage().Window.Maximize();
        }

        [AfterScenario]
        public void AfterScenario()
        {
            _driver?.Quit();
        }
    }
}