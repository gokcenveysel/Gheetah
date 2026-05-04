using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace {{ProjectName}}.Drivers
{
    public class DriverFactory
    {
        public static WindowsDriver CreateDriver(string appId)
        {
            var options = new AppiumOptions();
            options.App = appId; 
            options.AutomationName = "Windows";

            // WinAppDriver genellikle 4723 portunda çalışır
            var driver = new WindowsDriver(new Uri("http://127.0.0.1:4723"), options);

            // STABİLİTE AYARLARI
            // Masaüstü uygulamaları bazen geç yüklenir, bu yüzden bekleme kritik
            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
            
            // Pencereyi öne çek ve odaklan
            driver.Manage().Window.Maximize();

            return driver;
        }
    }
}