using OpenQA.Selenium.Appium.Windows;
using Reqnroll;
using FluentAssertions;
using {{ProjectName}}.Hooks;

namespace {{ProjectName}}.StepDefinitions
{
    [Binding]
    public class DesktopSteps
    {
        private readonly WindowsDriver _driver = (WindowsDriver)Hooks.Hooks.Driver;
		
		[Given(@"I launch the desktop application at path ""(.*)""")]
		public void LaunchApp(string appPath)
		{
			// Hooks içindeki statik Driver'ı burada parametre ile ayağa kaldırıyoruz
			if (Hooks.Hooks.Driver == null)
			{
				Hooks.Hooks.Driver = DriverFactory.CreateDriver(appPath);
			}
		}
        // --- Pencere & Uygulama Yönetimi ---
        [When(@"I maximize the desktop window")]
        public void Maximize() => _driver.Manage().Window.Maximize();

        [When(@"I switch to window with Title ""(.*)""")]
        public void SwitchWin(string title)
        {
             var handle = _driver.WindowHandles.FirstOrDefault(h => _driver.SwitchTo().Window(h).Title.Contains(title));
             _driver.SwitchTo().Window(handle);
        }

        // --- Element Etkileşimi ---
        [When(@"I click desktop element with (Name|AccessibilityId|XPath) ""(.*)""")]
        public void ClickDesktop(string type, string val) => GetDesktopElement(type, val).Click();

        [When(@"I type ""(.*)"" into desktop element (Name|AccessibilityId) ""(.*)""")]
        public void TypeDesktop(string text, string type, string val) 
        {
            var el = GetDesktopElement(type, val);
            el.Clear();
            el.SendKeys(text);
        }

        // --- Kısayollar (Hotkeys) ---
        [When(@"I press (Enter|Tab|Escape) key")]
        public void PressKey(string key)
        {
            var k = key switch { "Enter" => Keys.Enter, "Tab" => Keys.Tab, _ => Keys.Escape };
            new OpenQA.Selenium.Interactions.Actions(_driver).SendKeys(k).Perform();
        }

        // --- Doğrulamalar ---
        [Then(@"Desktop element (Name|AccessibilityId) ""(.*)"" should exist")]
        public void AssertExists(string type, string val) => GetDesktopElement(type, val).Should().NotBeNull();

        private WindowsElement GetDesktopElement(string type, string val)
        {
            return type switch {
                "Name" => _driver.FindElementByName(val),
                "AccessibilityId" => _driver.FindElementByAccessibilityId(val),
                "XPath" => _driver.FindElementByXPath(val),
                _ => throw new Exception("Invalid selector")
            };
        }
    }
}