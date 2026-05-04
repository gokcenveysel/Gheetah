using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Interfaces;
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
        private readonly AppiumDriver _driver;

        public MobileSteps()
        {
            _driver = (AppiumDriver)Hooks.Hooks.Driver!;
        }

        [Given(@"I launch (Android|iOS) application ""(.*)"" on device ""(.*)""")]
        public void LaunchMobileApp(string platform, string appIdentifier, string device)
        {
            if (Hooks.Hooks.Driver == null)
            {
                Hooks.Hooks.Driver = DriverFactory.CreateDriver(platform, appIdentifier, device);
            }
        }

        [When(@"I tap on mobile element with (ID|AccessibilityId|XPath|Text|ClassName) ""(.*)""")]
        public void Tap(string type, string val) => GetElement(type, val).Click();

        [When(@"I long press on mobile element with (ID|AccessibilityId|XPath) ""(.*)"" for (.*) seconds")]
        public void LongPress(string type, string val, int seconds)
        {
            var element = GetElement(type, val);
            new Actions(_driver)
                .ClickAndHold(element)
                .Pause(TimeSpan.FromSeconds(seconds))
                .Release()
                .Perform();
        }

        [When(@"I enter ""(.*)"" into mobile field (ID|AccessibilityId|XPath) ""(.*)""")]
        public void EnterText(string text, string type, string val)
        {
            var el = GetElement(type, val);
            el.Clear();
            el.SendKeys(text);
        }

        [When(@"I swipe (up|down|left|right)")]
        public void Swipe(string direction)
        {
            var size = _driver.Manage().Window.Size;
            int startX = size.Width / 2, startY = size.Height / 2;
            int endX = startX, endY = startY;

            switch (direction.ToLower())
            {
                case "up": startY = (int)(size.Height * 0.8); endY = (int)(size.Height * 0.2); break;
                case "down": startY = (int)(size.Height * 0.2); endY = (int)(size.Height * 0.8); break;
                case "left": startX = (int)(size.Width * 0.8); endX = (int)(size.Width * 0.2); break;
                case "right": startX = (int)(size.Width * 0.2); endX = (int)(size.Width * 0.8); break;
            }

            var finger = new PointerInputDevice(PointerKind.Touch);
            var sequence = new ActionSequence(finger, 0);
            sequence.AddAction(finger.CreatePointerMove(CoordinateOrigin.Viewport, startX, startY, TimeSpan.Zero));
            sequence.AddAction(finger.CreatePointerDown(MouseButton.Left));
            sequence.AddAction(finger.CreatePointerMove(CoordinateOrigin.Viewport, endX, endY, TimeSpan.FromMilliseconds(800)));
            sequence.AddAction(finger.CreatePointerUp(MouseButton.Left));
            _driver.PerformActions([sequence]);
        }

        [When(@"I hide keyboard")]
        public void HideKeyboard() => _driver.HideKeyboard();

        [When(@"I rotate device to (LANDSCAPE|PORTRAIT)")]
        public void Rotate(string mode) =>
            _driver.Orientation = mode == "LANDSCAPE" ? ScreenOrientation.Landscape : ScreenOrientation.Portrait;

        [When(@"I scroll until element with (AccessibilityId|XPath) ""(.*)"" is visible")]
        public void ScrollUntilVisible(string type, string val)
        {
            var element = GetElement(type, val);
        }

        // Assertions
        [Then(@"Mobile element with (ID|AccessibilityId|XPath) ""(.*)"" should (be visible|be enabled|exist)")]
        public void AssertMobileElement(string type, string val, string state)
        {
            var el = GetElement(type, val);
            if (state.Contains("visible")) el.Displayed.Should().BeTrue();
            else if (state.Contains("enabled")) el.Enabled.Should().BeTrue();
        }

        [Then(@"Mobile element with (ID|AccessibilityId|XPath) ""(.*)"" text should (be|contain) ""(.*)""")]
        public void AssertMobileText(string type, string val, string opt, string expected)
        {
            var actual = GetElement(type, val).Text.Trim();
            if (opt == "be") actual.Should().Be(expected);
            else actual.Should().Contain(expected);
        }

        private AppiumElement GetElement(string type, string val)
        {
            return type switch
            {
                "ID" => _driver.FindElement(MobileBy.Id(val)),
                "AccessibilityId" => _driver.FindElement(MobileBy.AccessibilityId(val)),
                "XPath" => _driver.FindElement(MobileBy.XPath(val)),
                "Text" => _driver.FindElement(MobileBy.AndroidUIAutomator($"new UiSelector().textContains(\"{val}\")")),
                "ClassName" => _driver.FindElement(MobileBy.ClassName(val)),
                _ => throw new ArgumentException($"Invalid mobile selector: {type}")
            };
        }
    }
}