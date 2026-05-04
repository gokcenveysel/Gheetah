using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using FluentAssertions;
using {{ProjectName}}.Hooks;
using System.Collections.ObjectModel;

namespace {{ProjectName}}.StepDefinitions
{
[Binding]
    public class WebSteps
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        public WebSteps()
        {
            _driver = Hooks.Hooks.Driver!;
            _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));
        }

        [Given(@"I navigate to URL ""(.*)""")]
        public void NavigateToUrl(string url) => _driver.Navigate().GoToUrl(url);

        [When(@"I maximize window")]
        public void MaximizeWindow() => _driver.Manage().Window.Maximize();

        [When(@"I refresh the page")]
        public void RefreshPage() => _driver.Navigate().Refresh();

        [When(@"I go back")]
        public void GoBack() => _driver.Navigate().Back();

        [When(@"I go forward")]
        public void GoForward() => _driver.Navigate().Forward();

        // ── Click Operations ─────────────────────────────────────
        [When(@"I click on element with (ID|Name|XPath|Css|Text|LinkText|Tag) ""(.*)""")]
        public void ClickElement(string type, string value)
        {
            var element = GetElement(type, value);
            _wait.Until(d => element.Displayed && element.Enabled);
            element.Click();
        }

        [When(@"I click on element with (ID|XPath|Css) ""(.*)"" using JS")]
        public void ClickWithJS(string type, string value)
        {
            var element = GetElement(type, value);
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
        }

        [When(@"I double click on element with (ID|XPath|Css) ""(.*)""")]
        public void DoubleClick(string type, string value)
        {
            var element = GetElement(type, value);
            new Actions(_driver).DoubleClick(element).Perform();
        }

        [When(@"I right click on element with (ID|XPath|Css) ""(.*)""")]
        public void RightClick(string type, string value)
        {
            var element = GetElement(type, value);
            new Actions(_driver).ContextClick(element).Perform();
        }

        // ── Input Operations ─────────────────────────────────────
        [When(@"I enter ""(.*)"" into element with (ID|Name|XPath|Css) ""(.*)""")]
        public void EnterText(string text, string type, string value)
        {
            var element = GetElement(type, value);
            element.Clear();
            element.SendKeys(text);
        }

        [When(@"I clear and enter ""(.*)"" into element with (ID|Name|XPath|Css) ""(.*)""")]
        public void ClearAndEnterText(string text, string type, string value)
        {
            var element = GetElement(type, value);
            element.Clear();
            element.SendKeys(text);
        }

        // ── Keyboard ─────────────────────────────────────────────
        [When(@"I press (Enter|Tab|Escape|Space|Backspace|Delete|F1|F2|F5) key")]
        public void PressKey(string key)
        {
            var keys = key switch
            {
                "Enter" => Keys.Enter,
                "Tab" => Keys.Tab,
                "Escape" => Keys.Escape,
                "Space" => Keys.Space,
                "Backspace" => Keys.Backspace,
                "Delete" => Keys.Delete,
                "F1" => Keys.F1,
                "F2" => Keys.F2,
                "F5" => Keys.F5,
                _ => Keys.Enter
            };
            new Actions(_driver).SendKeys(keys).Perform();
        }

        // ── Advanced Interactions ────────────────────────────────
        [When(@"I hover over element with (ID|XPath|Css) ""(.*)""")]
        public void HoverOver(string type, string value)
        {
            var element = GetElement(type, value);
            new Actions(_driver).MoveToElement(element).Perform();
        }

        [When(@"I scroll to element with (ID|XPath|Css) ""(.*)""")]
        public void ScrollToElement(string type, string value)
        {
            var element = GetElement(type, value);
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", element);
        }

        // ── Assertions ───────────────────────────────────────────
        [Then(@"Title should (be|contain) ""(.*)""")]
        public void AssertTitle(string option, string expected)
        {
            if (option == "be") _driver.Title.Should().Be(expected);
            else _driver.Title.Should().Contain(expected);
        }

        [Then(@"Element with (ID|Name|XPath|Css) ""(.*)"" should (be visible|be hidden|be enabled|be disabled|exist)")]
        public void AssertElementState(string type, string value, string state)
        {
            var element = GetElement(type, value, wait: state != "be hidden");

            switch (state)
            {
                case "be visible": element.Displayed.Should().BeTrue(); break;
                case "be hidden": element.Displayed.Should().BeFalse(); break;
                case "be enabled": element.Enabled.Should().BeTrue(); break;
                case "be disabled": element.Enabled.Should().BeFalse(); break;
                case "exist": element.Should().NotBeNull(); break;
            }
        }

        [Then(@"Element with (ID|XPath|Css) ""(.*)"" text should (be|contain) ""(.*)""")]
        public void AssertElementText(string type, string value, string option, string expected)
        {
            var actual = GetElement(type, value).Text.Trim();
            if (option == "be") actual.Should().Be(expected);
            else actual.Should().Contain(expected);
        }

        [Then(@"Element with (ID|XPath|Css) ""(.*)"" attribute ""(.*)"" should (be|contain) ""(.*)""")]
        public void AssertAttributeValue(string type, string value, string attribute, string option, string expected)
        {
            var actual = GetElement(type, value).GetAttribute(attribute);
            if (option == "be") actual.Should().Be(expected);
            else actual.Should().Contain(expected);
        }

        private IWebElement GetElement(string type, string value, bool wait = true)
        {
            By by = type switch
            {
                "ID" => By.Id(value),
                "Name" => By.Name(value),
                "XPath" => By.XPath(value),
                "Css" => By.CssSelector(value),
                "Text" => By.XPath($"//*[normalize-space()='{value}']"),
                "LinkText" => By.LinkText(value),
                "Tag" => By.TagName(value),
                _ => throw new ArgumentException($"Invalid selector type: {type}")
            };

            return wait ? _wait.Until(d => d.FindElement(by)) : _driver.FindElement(by);
        }
    }
}