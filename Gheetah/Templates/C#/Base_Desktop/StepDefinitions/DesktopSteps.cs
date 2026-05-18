using OpenQA.Selenium.Appium.Windows;
using OpenQA.Selenium.Interactions;
using Reqnroll;
using FluentAssertions;
using {{ProjectName}}.Hooks;
using System.Linq;

namespace {{ProjectName}}.StepDefinitions
{
    [Binding]
    public class DesktopSteps
    {
        private readonly WindowsDriver _driver;

        public DesktopSteps()
        {
            _driver = (WindowsDriver)Hooks.Hooks.Driver!;
        }

        [Given(@"I launch the desktop application at path ""(.*)""")]
        public void LaunchApp(string appPath)
        {
            if (Hooks.Hooks.Driver == null)
            {
                Hooks.Hooks.Driver = DriverFactory.CreateDriver(appPath);
            }
        }

        [When(@"I maximize the desktop window")]
        public void Maximize() => _driver.Manage().Window.Maximize();

        [When(@"I minimize the desktop window")]
        public void Minimize() => _driver.Manage().Window.Minimize();

        [When(@"I restore the desktop window")]
        public void Restore() => _driver.Manage().Window.Size = new System.Drawing.Size(1024, 768); // veya custom

        [When(@"I switch to window with Title ""(.*)""")]
        public void SwitchToWindow(string title)
        {
            var handle = _driver.WindowHandles.FirstOrDefault(h =>
            {
                _driver.SwitchTo().Window(h);
                return _driver.Title.Contains(title, StringComparison.OrdinalIgnoreCase);
            });

            handle.Should().NotBeNull($"Window with title containing '{title}' not found.");
            _driver.SwitchTo().Window(handle!);
        }

        [When(@"I close current window")]
        public void CloseWindow() => _driver.Close();

        // Click
        [When(@"I click desktop element with (Name|AccessibilityId|XPath|ClassName) ""(.*)""")]
        public void ClickDesktop(string type, string val) => GetElement(type, val).Click();

        [When(@"I right click desktop element with (Name|AccessibilityId|XPath) ""(.*)""")]
        public void RightClickDesktop(string type, string val)
        {
            var element = GetElement(type, val);
            new Actions(_driver).ContextClick(element).Perform();
        }

        // Input
        [When(@"I type ""(.*)"" into desktop element (Name|AccessibilityId|XPath) ""(.*)""")]
        public void TypeIntoDesktop(string text, string type, string val)
        {
            var el = GetElement(type, val);
            el.Clear();
            el.SendKeys(text);
        }

        [When(@"I clear desktop element (Name|AccessibilityId|XPath) ""(.*)""")]
        public void ClearDesktopElement(string type, string val) => GetElement(type, val).Clear();

        // Keyboard
        [When(@"I press (Enter|Tab|Escape|Space|F1|F2|F3|F4|F5|Delete) key")]
        public void PressKey(string key)
        {
            var keys = key switch
            {
                "Enter" => Keys.Enter,
                "Tab" => Keys.Tab,
                "Escape" => Keys.Escape,
                "Space" => Keys.Space,
                "Delete" => Keys.Delete,
                "F1" => Keys.F1,
                "F2" => Keys.F2,
                "F3" => Keys.F3,
                "F4" => Keys.F4,
                "F5" => Keys.F5,
                _ => throw new ArgumentException("Unsupported key")
            };
            new Actions(_driver).SendKeys(keys).Perform();
        }

        // Assertions
        [Then(@"Desktop element with (Name|AccessibilityId|XPath) ""(.*)"" should (exist|be visible|be enabled)")]
        public void AssertDesktopElement(string type, string val, string state)
        {
            var element = GetElement(type, val);
            switch (state)
            {
                case "exist":
                case "be visible":
                    element.Should().NotBeNull();
                    element.Displayed.Should().BeTrue();
                    break;
                case "be enabled":
                    element.Enabled.Should().BeTrue();
                    break;
            }
        }

        [Then(@"Desktop element with (Name|AccessibilityId) ""(.*)"" text should (be|contain) ""(.*)""")]
        public void AssertDesktopText(string type, string val, string opt, string expected)
        {
            var actual = GetElement(type, val).Text.Trim();
            if (opt == "be") actual.Should().Be(expected);
            else actual.Should().Contain(expected);
        }

        private WindowsElement GetElement(string type, string val)
        {
            return type switch
            {
                "Name" => _driver.FindElementByName(val),
                "AccessibilityId" => _driver.FindElementByAccessibilityId(val),
                "XPath" => _driver.FindElementByXPath(val),
                "ClassName" => _driver.FindElementByClassName(val),
                _ => throw new ArgumentException($"Invalid selector type: {type}")
            };
        }
    }
}