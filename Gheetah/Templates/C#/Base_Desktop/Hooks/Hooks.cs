using Reqnroll;
using OpenQA.Selenium.Appium;

namespace {{ProjectName}}.Hooks
{
    [Binding]
    public class Hooks
    {
        // Driver'ı statik tutup StepDefinition'dan doldurulmasını bekliyoruz
        public static AppiumDriver Driver { get; set; }

        [AfterScenario("@Mobile", "@Desktop")]
        public void Cleanup()
        {
            if (Driver != null)
            {
                Driver.Quit();
                Driver.Dispose();
                Driver = null;
            }
        }
    }
}