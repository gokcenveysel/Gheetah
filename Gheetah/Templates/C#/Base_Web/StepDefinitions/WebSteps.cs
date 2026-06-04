using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using FluentAssertions;
using {{ProjectName}}.Hooks;
using System.Threading;

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

        // ── Navigation ────────────────────────────────────────────
        [Given(@"I navigate to URL ""(.*)""")]
        [When(@"I navigate to URL ""(.*)""")]
        public void NavigateToUrl(string url) => _driver.Navigate().GoToUrl(url);

        [When(@"I maximize window")]
        public void MaximizeWindow() => _driver.Manage().Window.Maximize();

        [When(@"I minimize window")]
        public void MinimizeWindow() => _driver.Manage().Window.Minimize();

        [When(@"I refresh the page")]
        public void RefreshPage() => _driver.Navigate().Refresh();

        [When(@"I go back")]
        public void GoBack() => _driver.Navigate().Back();

        [When(@"I go forward")]
        public void GoForward() => _driver.Navigate().Forward();

        [When(@"I set window size to (\d+)x(\d+)")]
        public void SetWindowSize(int width, int height) =>
            _driver.Manage().Window.Size = new System.Drawing.Size(width, height);

        // ── Click Operations ─────────────────────────────────────
        [When(@"I click on element with (ID|Name|XPath|Css|Text|LinkText|Tag) ""(.*)""")]
        public void ClickElement(string type, string value)
        {
            var element = GetElement(type, value);
            _wait.Until(d => element.Displayed && element.Enabled);
            element.Click();
        }

        [When(@"I click on element with (ID|XPath|Css) ""(.*)"" using JS")]
        public void ClickWithJS(string type, string value) =>
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", GetElement(type, value));

        [When(@"I double click on element with (ID|XPath|Css|Text) ""(.*)""")]
        public void DoubleClick(string type, string value) =>
            new Actions(_driver).DoubleClick(GetElement(type, value)).Perform();

        [When(@"I right click on element with (ID|XPath|Css|Text) ""(.*)""")]
        public void RightClick(string type, string value) =>
            new Actions(_driver).ContextClick(GetElement(type, value)).Perform();

        [When(@"I click at coordinates (\d+), (\d+)")]
        public void ClickAtCoordinates(int x, int y) =>
            new Actions(_driver).MoveByOffset(x, y).Click().Perform();

        // ── Input Operations ─────────────────────────────────────
        [When(@"I enter ""(.*)"" into element with (ID|Name|XPath|Css) ""(.*)""")]
        public void EnterText(string text, string type, string value)
        {
            var el = GetElement(type, value);
            el.Clear();
            el.SendKeys(text);
        }

        [When(@"I append ""(.*)"" to element with (ID|Name|XPath|Css) ""(.*)""")]
        public void AppendText(string text, string type, string value) =>
            GetElement(type, value).SendKeys(text);

        [When(@"I clear element with (ID|Name|XPath|Css) ""(.*)""")]
        public void ClearElement(string type, string value) =>
            GetElement(type, value).Clear();

        [When(@"I type slowly ""(.*)"" into element with (ID|Name|XPath|Css) ""(.*)""")]
        public void TypeSlowly(string text, string type, string value)
        {
            var el = GetElement(type, value);
            el.Clear();
            foreach (char c in text)
            {
                el.SendKeys(c.ToString());
                Thread.Sleep(50);
            }
        }

        [When(@"I upload file ""(.*)"" to element with (ID|Name|XPath|Css) ""(.*)""")]
        public void UploadFile(string filePath, string type, string value) =>
            GetElement(type, value).SendKeys(filePath);

        // ── Dropdown / Select ─────────────────────────────────────
        [When(@"I select ""(.*)"" from dropdown with (ID|Name|XPath|Css) ""(.*)""")]
        public void SelectByText(string text, string type, string value) =>
            new SelectElement(GetElement(type, value)).SelectByText(text);

        [When(@"I select value ""(.*)"" from dropdown with (ID|Name|XPath|Css) ""(.*)""")]
        public void SelectByValue(string val, string type, string value) =>
            new SelectElement(GetElement(type, value)).SelectByValue(val);

        [When(@"I select index (\d+) from dropdown with (ID|Name|XPath|Css) ""(.*)""")]
        public void SelectByIndex(int index, string type, string value) =>
            new SelectElement(GetElement(type, value)).SelectByIndex(index);

        // ── Checkbox / Radio ──────────────────────────────────────
        [When(@"I check the (checkbox|radio) with (ID|Name|XPath|Css) ""(.*)""")]
        public void CheckElement(string kind, string type, string value)
        {
            var el = GetElement(type, value);
            if (!el.Selected) el.Click();
        }

        [When(@"I uncheck the checkbox with (ID|Name|XPath|Css) ""(.*)""")]
        public void UncheckElement(string type, string value)
        {
            var el = GetElement(type, value);
            if (el.Selected) el.Click();
        }

        // ── Keyboard ─────────────────────────────────────────────
        [When(@"I press (Enter|Tab|Escape|Space|Backspace|Delete|F1|F2|F5|ArrowUp|ArrowDown|ArrowLeft|ArrowRight|Home|End|PageUp|PageDown) key")]
        public void PressKey(string key)
        {
            var k = key switch
            {
                "Enter" => Keys.Enter, "Tab" => Keys.Tab, "Escape" => Keys.Escape,
                "Space" => Keys.Space, "Backspace" => Keys.Backspace, "Delete" => Keys.Delete,
                "F1" => Keys.F1, "F2" => Keys.F2, "F5" => Keys.F5,
                "ArrowUp" => Keys.ArrowUp, "ArrowDown" => Keys.ArrowDown,
                "ArrowLeft" => Keys.ArrowLeft, "ArrowRight" => Keys.ArrowRight,
                "Home" => Keys.Home, "End" => Keys.End,
                "PageUp" => Keys.PageUp, "PageDown" => Keys.PageDown,
                _ => Keys.Enter
            };
            new Actions(_driver).SendKeys(k).Perform();
        }

        [When(@"I press (Ctrl|Alt|Shift)\+(.+) key combination")]
        public void PressKeyCombination(string modifier, string key)
        {
            var modKey = modifier switch { "Ctrl" => Keys.Control, "Alt" => Keys.Alt, _ => Keys.Shift };
            new Actions(_driver).KeyDown(modKey).SendKeys(key.ToLower()).KeyUp(modKey).Perform();
        }

        [When(@"I press key on element with (ID|XPath|Css) ""(.*)"": (Enter|Tab|Escape|Space|Backspace|Delete)")]
        public void PressKeyOnElement(string type, string value, string key)
        {
            var k = key switch
            {
                "Enter" => Keys.Enter, "Tab" => Keys.Tab, "Escape" => Keys.Escape,
                "Space" => Keys.Space, "Backspace" => Keys.Backspace, _ => Keys.Delete
            };
            GetElement(type, value).SendKeys(k);
        }

        // ── Mouse / Advanced Interactions ────────────────────────
        [When(@"I hover over element with (ID|XPath|Css|Text) ""(.*)""")]
        public void HoverOver(string type, string value) =>
            new Actions(_driver).MoveToElement(GetElement(type, value)).Perform();

        [When(@"I scroll to element with (ID|XPath|Css|Text) ""(.*)""")]
        public void ScrollToElement(string type, string value) =>
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block:'center'});", GetElement(type, value));

        [When(@"I scroll to (top|bottom) of page")]
        public void ScrollPage(string position) =>
            ((IJavaScriptExecutor)_driver).ExecuteScript(
                position == "top" ? "window.scrollTo(0,0);" : "window.scrollTo(0, document.body.scrollHeight);");

        [When(@"I scroll down (\d+) pixels")]
        public void ScrollByPixels(int pixels) =>
            ((IJavaScriptExecutor)_driver).ExecuteScript($"window.scrollBy(0, {pixels});");

        [When(@"I drag element with (ID|XPath|Css) ""(.*)"" and drop to element with (ID|XPath|Css) ""(.*)""")]
        public void DragAndDrop(string srcType, string srcVal, string tgtType, string tgtVal) =>
            new Actions(_driver).DragAndDrop(GetElement(srcType, srcVal), GetElement(tgtType, tgtVal)).Perform();

        // ── Alerts / Dialogs ──────────────────────────────────────
        [When(@"I accept the alert")]
        public void AcceptAlert()
        {
            _wait.Until(d => { try { d.SwitchTo().Alert(); return true; } catch { return false; } });
            _driver.SwitchTo().Alert().Accept();
        }

        [When(@"I dismiss the alert")]
        public void DismissAlert()
        {
            _wait.Until(d => { try { d.SwitchTo().Alert(); return true; } catch { return false; } });
            _driver.SwitchTo().Alert().Dismiss();
        }

        [When(@"I enter ""(.*)"" in the alert and accept")]
        public void EnterTextInAlert(string text)
        {
            _wait.Until(d => { try { d.SwitchTo().Alert(); return true; } catch { return false; } });
            var alert = _driver.SwitchTo().Alert();
            alert.SendKeys(text);
            alert.Accept();
        }

        [Then(@"Alert text should (be|contain) ""(.*)""")]
        public void AssertAlertText(string option, string expected)
        {
            var alertText = _wait.Until(d => { try { return d.SwitchTo().Alert().Text; } catch { return null; } });
            if (option == "be") alertText.Should().Be(expected);
            else alertText.Should().Contain(expected);
        }

        // ── Frames / iFrames ──────────────────────────────────────
        [When(@"I switch to frame with (ID|Name) ""(.*)""")]
        public void SwitchToFrameByIdOrName(string type, string value) =>
            _driver.SwitchTo().Frame(type == "ID" ? _driver.FindElement(By.Id(value)) : _driver.FindElement(By.Name(value)));

        [When(@"I switch to frame with XPath ""(.*)""")]
        public void SwitchToFrameByXPath(string xpath) =>
            _driver.SwitchTo().Frame(_driver.FindElement(By.XPath(xpath)));

        [When(@"I switch to frame index (\d+)")]
        public void SwitchToFrameByIndex(int index) => _driver.SwitchTo().Frame(index);

        [When(@"I switch to default content")]
        public void SwitchToDefault() => _driver.SwitchTo().DefaultContent();

        [When(@"I switch to parent frame")]
        public void SwitchToParentFrame() => _driver.SwitchTo().ParentFrame();

        // ── Windows / Tabs ────────────────────────────────────────
        [When(@"I open new tab")]
        public void OpenNewTab() => ((IJavaScriptExecutor)_driver).ExecuteScript("window.open('','_blank');");

        [When(@"I switch to tab (\d+)")]
        public void SwitchToTab(int index) => _driver.SwitchTo().Window(_driver.WindowHandles[index]);

        [When(@"I close current tab")]
        public void CloseCurrentTab() => _driver.Close();

        [When(@"I switch to main window")]
        public void SwitchToMainWindow() => _driver.SwitchTo().Window(_driver.WindowHandles[0]);

        // ── Wait ──────────────────────────────────────────────────
        [When(@"I wait (\d+) seconds")]
        public void WaitSeconds(int seconds) => Thread.Sleep(seconds * 1000);

        [When(@"I wait (\d+) milliseconds")]
        public void WaitMilliseconds(int ms) => Thread.Sleep(ms);

        [When(@"I wait for element with (ID|XPath|Css) ""(.*)"" to (be visible|disappear)")]
        public void WaitForElement(string type, string value, string state)
        {
            By by = type switch { "ID" => By.Id(value), "XPath" => By.XPath(value), _ => By.CssSelector(value) };
            if (state == "be visible")
                _wait.Until(d => { try { return d.FindElement(by).Displayed; } catch { return false; } });
            else
                _wait.Until(d => { try { return !d.FindElement(by).Displayed; } catch (NoSuchElementException) { return true; } });
        }

        [When(@"I wait for URL to contain ""(.*)""")]
        public void WaitForUrl(string partial) =>
            _wait.Until(d => d.Url.Contains(partial));

        [When(@"I wait for page title to contain ""(.*)""")]
        public void WaitForTitle(string partial) =>
            _wait.Until(d => d.Title.Contains(partial));

        // ── JavaScript Execution ──────────────────────────────────
        [When(@"I execute JavaScript ""(.*)""")]
        public void ExecuteJS(string script) =>
            ((IJavaScriptExecutor)_driver).ExecuteScript(script);

        [When(@"I set attribute ""(.*)"" of element with (ID|XPath|Css) ""(.*)"" to ""(.*)""")]
        public void SetAttribute(string attr, string type, string value, string attrValue) =>
            ((IJavaScriptExecutor)_driver).ExecuteScript(
                $"arguments[0].setAttribute('{attr}', '{attrValue}');", GetElement(type, value));

        [When(@"I highlight element with (ID|XPath|Css) ""(.*)""")]
        public void HighlightElement(string type, string value) =>
            ((IJavaScriptExecutor)_driver).ExecuteScript(
                "arguments[0].style.border='3px solid red';", GetElement(type, value));

        // ── Cookies ───────────────────────────────────────────────
        [When(@"I add cookie with name ""(.*)"" and value ""(.*)""")]
        public void AddCookie(string name, string value) =>
            _driver.Manage().Cookies.AddCookie(new Cookie(name, value));

        [When(@"I delete cookie with name ""(.*)""")]
        public void DeleteCookie(string name) =>
            _driver.Manage().Cookies.DeleteCookieNamed(name);

        [When(@"I delete all cookies")]
        public void DeleteAllCookies() => _driver.Manage().Cookies.DeleteAllCookies();

        [Then(@"Cookie ""(.*)"" should (exist|not exist)")]
        public void AssertCookieExists(string name, string state)
        {
            var cookie = _driver.Manage().Cookies.GetCookieNamed(name);
            if (state == "exist") cookie.Should().NotBeNull();
            else cookie.Should().BeNull();
        }

        // ── Assertions ───────────────────────────────────────────
        [Then(@"Title should (be|contain) ""(.*)""")]
        public void AssertTitle(string option, string expected)
        {
            if (option == "be") _driver.Title.Should().Be(expected);
            else _driver.Title.Should().Contain(expected);
        }

        [Then(@"Current URL should (be|contain|end with) ""(.*)""")]
        public void AssertUrl(string option, string expected)
        {
            if (option == "be") _driver.Url.Should().Be(expected);
            else if (option == "contain") _driver.Url.Should().Contain(expected);
            else _driver.Url.Should().EndWith(expected);
        }

        [Then(@"Page source should (contain|not contain) ""(.*)""")]
        public void AssertPageSource(string option, string text)
        {
            if (option == "contain") _driver.PageSource.Should().Contain(text);
            else _driver.PageSource.Should().NotContain(text);
        }

        [Then(@"Element with (ID|Name|XPath|Css|Text) ""(.*)"" should (be visible|be hidden|be enabled|be disabled|exist|not exist)")]
        public void AssertElementState(string type, string value, string state)
        {
            if (state == "not exist")
            {
                _driver.FindElements(GetBy(type, value)).Count.Should().Be(0);
                return;
            }
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

        [Then(@"Element with (ID|XPath|Css|Text) ""(.*)"" text should (be|contain|start with|end with) ""(.*)""")]
        public void AssertElementText(string type, string value, string option, string expected)
        {
            var actual = GetElement(type, value).Text.Trim();
            switch (option)
            {
                case "be": actual.Should().Be(expected); break;
                case "contain": actual.Should().Contain(expected); break;
                case "start with": actual.Should().StartWith(expected); break;
                case "end with": actual.Should().EndWith(expected); break;
            }
        }

        [Then(@"Element with (ID|XPath|Css) ""(.*)"" attribute ""(.*)"" should (be|contain|not be empty) ""?(.*)""?")]
        public void AssertAttributeValue(string type, string value, string attribute, string option, string expected)
        {
            var actual = GetElement(type, value).GetAttribute(attribute);
            if (option == "not be empty") { actual.Should().NotBeNullOrEmpty(); return; }
            if (option == "be") actual.Should().Be(expected);
            else actual.Should().Contain(expected);
        }

        [Then(@"Element with (ID|XPath|Css) ""(.*)"" CSS property ""(.*)"" should (be|contain) ""(.*)""")]
        public void AssertCssValue(string type, string value, string property, string option, string expected)
        {
            var actual = GetElement(type, value).GetCssValue(property);
            if (option == "be") actual.Should().Be(expected);
            else actual.Should().Contain(expected);
        }

        [Then(@"Element count with (XPath|Css) ""(.*)"" should be (\d+)")]
        public void AssertElementCount(string type, string value, int expected) =>
            _driver.FindElements(GetBy(type, value)).Count.Should().Be(expected);

        [Then(@"Element count with (XPath|Css) ""(.*)"" should be greater than (\d+)")]
        public void AssertElementCountGreaterThan(string type, string value, int min) =>
            _driver.FindElements(GetBy(type, value)).Count.Should().BeGreaterThan(min);

        [Then(@"Dropdown with (ID|Name|XPath|Css) ""(.*)"" selected option should (be|contain) ""(.*)""")]
        public void AssertDropdownSelection(string type, string value, string option, string expected)
        {
            var selectedText = new SelectElement(GetElement(type, value)).SelectedOption.Text;
            if (option == "be") selectedText.Should().Be(expected);
            else selectedText.Should().Contain(expected);
        }

        [Then(@"Checkbox with (ID|Name|XPath|Css) ""(.*)"" should be (checked|unchecked)")]
        public void AssertCheckboxState(string type, string value, string state)
        {
            var isChecked = GetElement(type, value).Selected;
            if (state == "checked") isChecked.Should().BeTrue();
            else isChecked.Should().BeFalse();
        }

        // ── Helpers ───────────────────────────────────────────────
        private By GetBy(string type, string value) => type switch
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

        private IWebElement GetElement(string type, string value, bool wait = true) =>
            wait ? _wait.Until(d => d.FindElement(GetBy(type, value))) : _driver.FindElement(GetBy(type, value));
    }
}
