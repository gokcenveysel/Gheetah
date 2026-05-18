using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace {{ProjectName}}.Drivers
{
    public static class DriverFactory
    {
        public static WindowsDriver CreateDriver(string appPath, string deviceName = "WindowsPC")
        {
            var options = new AppiumOptions();
            options.AddAdditionalAppiumOption("app", appPath);
            options.AddAdditionalAppiumOption("deviceName", deviceName);
            options.AddAdditionalAppiumOption("platformName", "Windows");
            options.AddAdditionalAppiumOption("ms:waitForAppLaunch", "10");

            var driver = new WindowsDriver(new Uri("http://127.0.0.1:4723"), options);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
            return driver;
        }
    }
}