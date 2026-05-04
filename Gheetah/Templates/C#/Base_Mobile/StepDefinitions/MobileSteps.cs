using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Interactions;
using OpenQA.Selenium.Interactions;
using Reqnroll;
using FluentAssertions;
using {{ProjectName}}.Hooks;

namespace {{ProjectName}}.StepDefinitions
{
    [Binding]
    public class MobileSteps
    {
        private readonly AppiumDriver? _driver = (AppiumDriver)Hooks.Hooks.Driver;
		
		[Given(@"I launch (Android|iOS) application ""(.*)"" on device ""(.*)""")]
		public void LaunchMobileApp(string platform, string appIdentifier, string device)
		{
			if (Hooks.Hooks.Driver == null)
			{
				Hooks.Hooks.Driver = DriverFactory.CreateDriver(platform, appIdentifier, device);
			}
		}

        [When(@"I tap on mobile element with (ID|AccessibilityId|XPath|Text) ""(.*)""")]
        public void TapMobile(string type, string val) => GetMobileElement(type, val).Click();

        [When(@"I enter ""(.*)"" into mobile field (ID|AccessibilityId) ""(.*)""")]
        public void EnterMobile(string text, string type, string val) 
        {
            var el = GetMobileElement(type, val);
            el.Clear();
            el.SendKeys(text);
        }

        [When(@"I swipe (up|down|left|right)")]
        public void Swipe(string direction)
        {
            var size = _driver.Manage().Window.Size;
            int startX = size.Width / 2, startY = size.Height / 2, endX = startX, endY = startY;

            switch(direction.ToLower()) {
                case "up": startY = (int)(size.Height * 0.8); endY = (int)(size.Height * 0.2); break;
                case "down": startY = (int)(size.Height * 0.2); endY = (int)(size.Height * 0.8); break;
                case "left": startX = (int)(size.Width * 0.8); endX = (int)(size.Width * 0.2); break;
                case "right": startX = (int)(size.Width * 0.2); endX = (int)(size.Width * 0.8); break;
            }
            var finger = new PointerInputDevice(PointerKind.Touch);
            var sequence = new ActionSequence(finger);
            sequence.AddAction(finger.CreatePointerMove(CoordinateOrigin.Viewport, startX, startY, TimeSpan.Zero));
            sequence.AddAction(finger.CreatePointerDown(MouseButton.Left));
            sequence.AddAction(finger.CreatePointerMove(CoordinateOrigin.Viewport, endX, endY, TimeSpan.FromMilliseconds(500)));
            sequence.AddAction(finger.CreatePointerUp(MouseButton.Left));
            _driver.PerformActions(new List<ActionSequence> { sequence });
        }

        [When(@"I hide keyboard")]
        public void HideKey() => _driver.HideKeyboard();

        [When(@"I rotate device to (LANDSCAPE|PORTRAIT)")]
        public void Rotate(string mode) => _driver.Orientation = mode == "LANDSCAPE" ? ScreenOrientation.Landscape : ScreenOrientation.Portrait;

        [Then(@"Mobile element (ID|AccessibilityId) ""(.*)"" should (be visible|be enabled)")]
        public void AssertMobileState(string type, string val, string state)
        {
            var el = GetMobileElement(type, val);
            if (state == "be visible") el.Displayed.Should().BeTrue(); else el.Enabled.Should().BeTrue();
        }

        private AppiumElement GetMobileElement(string type, string val)
        {
            return type switch {
                "ID" => _driver.FindElement(MobileBy.Id(val)),
                "AccessibilityId" => _driver.FindElement(MobileBy.AccessibilityId(val)),
                "XPath" => _driver.FindElement(MobileBy.XPath(val)),
                "Text" => _driver.FindElement(MobileBy.AndroidUIAutomator($"new UiSelector().text(\"{val}\")")),
                _ => throw new Exception("Invalid selector")
            };
        }
    }
}