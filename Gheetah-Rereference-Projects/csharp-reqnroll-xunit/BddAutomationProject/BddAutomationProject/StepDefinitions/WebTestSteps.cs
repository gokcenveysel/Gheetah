using BddAutomationProject.Drivers;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using Reqnroll;
using Xunit;

namespace BddAutomationProject.StepDefinitions
{
    [Binding]
    public class WebTestSteps
    {
        private readonly IWebDriver _driver;
        private readonly ScenarioContext _scenarioContext;

        public WebTestSteps(ScenarioContext scenarioContext, WebDriverFactory driverFactory)
        {
            _scenarioContext = scenarioContext;
            _driver = driverFactory.GetDriver();
        }

        [Given(@"I am on the Add/Remove Elements page")]
        public void GivenIAmOnTheAddRemoveElementsPage()
        {
            _driver.Navigate().GoToUrl("https://the-internet.herokuapp.com/add_remove_elements/");
        }

        [When(@"I add (.*) elements")]
        public void WhenIAddElements(int count)
        {
            var addButton = _driver.FindElement(By.CssSelector("button[onclick='addElement()']"));
            for (int i = 0; i < count; i++)
            {
                addButton.Click();
            }
        }

        [When(@"I remove (.*) elements")]
        public void WhenIRemoveElements(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var deleteButton = _driver.FindElement(By.CssSelector(".added-manually"));
                deleteButton.Click();
            }
        }

        [Then(@"(.*) element should remain")]
        public void ThenElementShouldRemain(int expectedCount)
        {
            var elements = _driver.FindElements(By.CssSelector(".added-manually"));
            Assert.Equal(expectedCount, elements.Count);
        }

        [Given(@"I am on the Drag and Drop page")]
        public void GivenIAmOnTheDragAndDropPage()
        {
            _driver.Navigate().GoToUrl("https://the-internet.herokuapp.com/drag_and_drop");
        }

        [When(@"I drag element A to element B")]
        public void WhenIDragElementAToElementB()
        {
            var elementA = _driver.FindElement(By.Id("column-a"));
            var elementB = _driver.FindElement(By.Id("column-b"));
            new Actions(_driver).DragAndDrop(elementA, elementB).Perform();
        }

        [Then(@"element B should contain element A")]
        public void ThenElementBShouldContainElementA()
        {
            var elementB = _driver.FindElement(By.Id("column-b"));
            Assert.Equal("A", elementB.Text);
        }

        [Given(@"I am on the Floating Menu page")]
        public void GivenIAmOnTheFloatingMenuPage()
        {
            _driver.Navigate().GoToUrl("https://the-internet.herokuapp.com/floating_menu");
        }

        [When(@"I scroll to the bottom of the page")]
        public void WhenIScrollToTheBottomOfThePage()
        {
            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript("window.scrollTo(0, document.body.scrollHeight)");
        }

        [Then(@"the floating menu should still be visible")]
        public void ThenTheFloatingMenuShouldStillBeVisible()
        {
            var menu = _driver.FindElement(By.Id("menu"));
            Assert.True(menu.Displayed);
        }
    }
}