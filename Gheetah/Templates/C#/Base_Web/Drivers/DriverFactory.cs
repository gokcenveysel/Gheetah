using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Edge;

namespace {{ProjectName}}.Drivers
{
    public class DriverFactory
    {
        public static IWebDriver CreateDriver()
        {
            // İleride burayı config dosyasından okuyacak şekilde geliştirebilirsin
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("--start-maximized");
            
            // CI/CD süreçlerinde hata almamak için headless opsiyonu eklenebilir
            // chromeOptions.AddArgument("--headless"); 

            return new ChromeDriver(chromeOptions);
        }
    }
}