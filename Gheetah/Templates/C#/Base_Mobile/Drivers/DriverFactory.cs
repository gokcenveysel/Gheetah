using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;

namespace {{ProjectName}}.Drivers
{
    public class DriverFactory
    {
        public static AppiumDriver CreateDriver(string platform, string appPathOrBundleId, string deviceName = "GheetahDevice")
        {
            var options = new AppiumOptions();
            options.DeviceName = deviceName;
            options.AutomationName = platform.ToLower() == "android" ? "UiAutomator2" : "XCUITest";
            
            // Eğer appPathOrBundleId bir dosya yoluysa (.apk/.app) App özelliğine set et, 
            // değilse (com.apple.mobilesafari vb.) direkt bundleId/appPackage olarak değerlendirilir.
            if (appPathOrBundleId.Contains("/") || appPathOrBundleId.Contains("\\"))
                options.App = appPathOrBundleId;
            else
                options.AddAdditionalAppiumOption(platform.ToLower() == "android" ? "appPackage" : "bundleId", appPathOrBundleId);

            var serverUri = new Uri("http://127.0.0.1:4723/");

            AppiumDriver driver = platform.ToLower() == "android" 
                ? new AndroidDriver(serverUri, options) 
                : new IOSDriver(serverUri, options);

            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
            return driver;
        }
    }
}