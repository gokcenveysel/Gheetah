using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.Enums;
using OpenQA.Selenium.Appium.iOS;

namespace {{ProjectName}}.Drivers
{
    public static class DriverFactory
    {
        public static AppiumDriver CreateAndroidDriver(string appPackage, string appActivity, string deviceName = "emulator-5554")
        {
            var options = new AppiumOptions();
            options.AddAdditionalAppiumOption(MobileCapabilityType.PlatformName, "Android");
            options.AddAdditionalAppiumOption(MobileCapabilityType.AutomationName, "UiAutomator2");
            options.AddAdditionalAppiumOption(MobileCapabilityType.DeviceName, deviceName);
            options.AddAdditionalAppiumOption(MobileCapabilityType.AppPackage, appPackage);
            options.AddAdditionalAppiumOption(MobileCapabilityType.AppActivity, appActivity);

            var driver = new AndroidDriver(new Uri("http://127.0.0.1:4723"), options);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(15);
            return driver;
        }

        public static AppiumDriver CreateIosDriver(string bundleId, string deviceName = "iPhone 15")
        {
            var options = new AppiumOptions();
            options.AddAdditionalAppiumOption(MobileCapabilityType.PlatformName, "iOS");
            options.AddAdditionalAppiumOption(MobileCapabilityType.AutomationName, "XCUITest");
            options.AddAdditionalAppiumOption(MobileCapabilityType.DeviceName, deviceName);
            options.AddAdditionalAppiumOption(MobileCapabilityType.BundleId, bundleId);

            var driver = new IOSDriver(new Uri("http://127.0.0.1:4723"), options);
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(15);
            return driver;
        }
    }
}