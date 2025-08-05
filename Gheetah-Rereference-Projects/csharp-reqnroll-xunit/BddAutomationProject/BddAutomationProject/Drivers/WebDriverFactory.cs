using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace BddAutomationProject.Drivers
{
    public class WebDriverFactory
    {
        private IWebDriver _driver;

        public IWebDriver GetDriver()
        {
            if (_driver == null)
            {
                var options = new ChromeOptions();
                options.AddArgument("--headless"); // Optional: Run in headless mode
                _driver = new ChromeDriver(options);
            }
            return _driver;
        }
    }
}