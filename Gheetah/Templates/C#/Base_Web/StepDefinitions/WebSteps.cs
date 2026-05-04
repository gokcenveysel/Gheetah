using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using FluentAssertions;
using {{ProjectName}}.Hooks;

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
        public void Nav(string url) => _driver.Navigate().GoToUrl(url);

        [When(@"I maximize window")]
        public void Max() => _driver.Manage().Window.Maximize();


        [When(@"I click on element with (ID|Name|XPath|Css|Text) ""(.*)""")]
        public void Click(string type, string val) 
        {
            var element = GetElement(type, val);
            element.Click(); 
        }

        [When(@"I click on element with (ID|XPath) ""(.*)"" using JS")]
        public void ClickJS(string type, string val) 
        {
            var element = GetElement(type, val);
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
        }

        [When(@"I double click on element with (ID|XPath) ""(.*)""")]
        public void DoubleClick(string type, string val) 
        {
            var element = GetElement(type, val);
            new Actions(_driver).DoubleClick(element).Perform();
        }

        [When(@"I enter ""(.*)"" into element with (ID|Name|XPath) ""(.*)""")]
        public void Input(string text, string type, string val) 
        {
            var element = GetElement(type, val);
            element.SendKeys(text);
        }

        [When(@"I clear and enter ""(.*)"" into element with ID ""(.*)""")]
        public void ClearInput(string text, string id) 
        { 
            var e = GetElement("ID", id); 
            e.Clear(); 
            e.SendKeys(text); 
        }

        [Then(@"Title should (be|contain) ""(.*)""")]
        public void AssertTitle(string opt, string val) 
        {
            if (opt == "be") _driver.Title.Should().Be(val); 
            else _driver.Title.Should().Contain(val);
        }

        [Then(@"Element with (ID|XPath) ""(.*)"" should (be visible|be hidden|be enabled)")]
        public void AssertState(string type, string val, string state)
        {
            var e = GetElement(type, val);
            if (state == "be visible") e.Displayed.Should().BeTrue();
            else if (state == "be hidden") e.Displayed.Should().BeFalse();
            else e.Enabled.Should().BeTrue();
        }

        private IWebElement GetElement(string type, string val)
        {
            By by = type switch {
                "ID" => By.Id(val), 
                "Name" => By.Name(val), 
                "XPath" => By.XPath(val),
                "Css" => By.CssSelector(val), 
                "Text" => By.XPath($"//*[text()='{val}']"),
                _ => throw new ArgumentException("Invalid selector type")
            };
            return _wait.Until(d => d.FindElement(by));
        }
    }
}