using OpenQA.Selenium;
using Reqnroll;
using {{ProjectName}}.Drivers;

namespace {{ProjectName}}.Hooks
{
    [Binding]
    public class Hooks
    {
        private readonly ScenarioContext _scenarioContext;
        public static IWebDriver? Driver { get; set; }

        public Hooks(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
        }

        [BeforeScenario]
        public void BeforeScenario()
        {
            Console.WriteLine($"Scenario Started: {_scenarioContext.ScenarioInfo.Title}");

            if (Driver == null)
            {
                Driver = DriverFactory.CreateDriver();
            }
        }

        [AfterScenario]
        public void AfterScenario()
        {
            if (_scenarioContext.TestError != null)
            {
                TakeScreenshot(_scenarioContext.ScenarioInfo.Title);
            }

            QuitDriver();
        }

        private void TakeScreenshot(string scenarioName)
        {
            try
            {
                if (Driver is ITakesScreenshot ts)
                {
                    var screenshot = ts.GetScreenshot();
                    var fileName = $"Web_{scenarioName}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                    var path = Path.Combine("Screenshots", fileName);

                    Directory.CreateDirectory("Screenshots");
                    screenshot.SaveAsFile(path);
                    Console.WriteLine($"Screenshot saved: {path}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Screenshot error: {ex.Message}");
            }
        }

        private void QuitDriver()
        {
            try
            {
                Driver?.Quit();
                Driver?.Dispose();
                Driver = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Driver quit error: {ex.Message}");
            }
        }
    }
}