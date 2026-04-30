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
        private readonly IWebDriver _driver = Hooks.Hooks.Driver;
        private readonly WebDriverWait _wait = new(Hooks.Hooks.Driver, TimeSpan.FromSeconds(20));

        // --- Navigasyon & Pencere ---
        [Given(@"I navigate to URL ""(.*)""")]
        public void Nav(string url) => _driver.Navigate().GoToUrl(url);

        [When(@"I maximize window")]
        public void Max() => _driver.Manage().Window.Maximize();

        // --- Tıklama (Parametrik Seçicilerle) ---
        [When(@"I click on element with (ID|Name|XPath|Css|Text) ""(.*)""")]
        public void Click(string type, string val) => GetElement(type, val).Click();

        [When(@"I click on element with (ID|XPath) ""(.*)"" using JS")]
        public void ClickJS(string type, string val) => ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", GetElement(type, val));

        [When(@"I double click on element with (ID|XPath) ""(.*)""")]
        public void DoubleClick(string type, string val) => new Actions(_driver).DoubleClick(GetElement(type, val)).Perform();

        // --- Veri Girişi ---
        [When(@"I enter ""(.*)"" into element with (ID|Name|XPath) ""(.*)""")]
        public void Input(string text, string type, string val) => GetElement(type, val).SendKeys(text);

        [When(@"I clear and enter ""(.*)"" into element with ID ""(.*)""")]
        public void ClearInput(string text, string id) { var e = GetElement("ID", id); e.Clear(); e.SendKeys(text); }

        // --- Dropdown & Select ---
        [When(@"I select ""(.*)"" from dropdown (ID|XPath) ""(.*)""")]
        public void Select(string text, string type, string val) => new SelectElement(GetElement(type, val)).SelectByText(text);

        // --- Doğrulamalar (Assertions) ---
        [Then(@"Title should (be|contain) ""(.*)""")]
        public void AssertTitle(string opt, string val) => opt == "be" ? _driver.Title.Should().Be(val) : _driver.Title.Should().Contain(val);

        [Then(@"Element with (ID|XPath) ""(.*)"" should (be visible|be hidden|be enabled)")]
        public void AssertState(string type, string val, string state)
        {
            var e = GetElement(type, val);
            if (state == "be visible") e.Displayed.Should().BeTrue();
            else if (state == "be hidden") e.Displayed.Should().BeFalse();
            else e.Enabled.Should().BeTrue();
        }

        [Then(@"Element with (ID|XPath) ""(.*)"" text should (equal|contain) ""(.*)""")]
        public void AssertText(string type, string val, string opt, string expected)
        {
            var text = GetElement(type, val).Text;
            if (opt == "equal") text.Should().Be(expected); else text.Should().Contain(expected);
        }

        // --- Helper: Dinamik Element Bulucu ---
        private IWebElement GetElement(string type, string val)
        {
            By by = type switch {
                "ID" => By.Id(val), "Name" => By.Name(val), "XPath" => By.XPath(val),
                "Css" => By.CssSelector(val), "Text" => By.XPath($"//*[text()='{val}']"),
                _ => throw new ArgumentException("Invalid selector type")
            };
            return _wait.Until(d => d.FindElement(by));
        }
    }
}