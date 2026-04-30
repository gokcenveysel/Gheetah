package {{ProjectName}}.stepdefinitions;

import {{ProjectName}}.drivers.DriverFactory;
import io.cucumber.java.en.*;
import org.openqa.selenium.*;
import org.openqa.selenium.interactions.Actions;
import org.openqa.selenium.support.ui.ExpectedConditions;
import org.openqa.selenium.support.ui.Select;
import org.openqa.selenium.support.ui.WebDriverWait;
import static org.assertj.core.api.Assertions.assertThat;
import java.time.Duration;
import java.util.List;

public class WebSteps {
    private final WebDriver driver = DriverFactory.getDriver();
    private final WebDriverWait wait = new WebDriverWait(driver, Duration.ofSeconds(20));

    @Given("I navigate to URL {string}")
    public void navigateTo(String url) { driver.get(url); }

    @When("I refresh the page")
    public void refresh() { driver.navigate().refresh(); }

    @When("I click on element with {string} {string}")
    public void clickElement(String type, String val) { getEl(type, val).click(); }

    @When("I click on element with {string} {string} using JS")
    public void clickJS(String type, String val) {
        ((JavascriptExecutor) driver).executeScript("arguments[0].click();", getEl(type, val));
    }

    @When("I double click on element with {string} {string}")
    public void doubleClick(String type, String val) {
        new Actions(driver).doubleClick(getEl(type, val)).perform();
    }

    @When("I hover over element with {string} {string}")
    public void hover(String type, String val) {
        new Actions(driver).moveToElement(getEl(type, val)).perform();
    }

    @When("I enter {string} into element with {string} {string}")
    public void enterText(String text, String type, String val) {
        WebElement el = getEl(type, val);
        el.clear();
        el.sendKeys(text);
    }

    @When("I select {string} from dropdown {string} {string}")
    public void selectFromDropdown(String text, String type, String val) {
        new Select(getEl(type, val)).selectByVisibleText(text);
    }

    @When("I switch to frame {string}")
    public void switchFrame(String id) { driver.switchTo().frame(id); }

    @When("I switch to default content")
    public void switchDefault() { driver.switchTo().defaultContent(); }

    @When("I accept the alert")
    public void acceptAlert() { driver.switchTo().alert().accept(); }

    @Then("Page title should {string} {string}")
    public void assertTitle(String condition, String expected) {
        if(condition.equals("be")) assertThat(driver.getTitle()).isEqualTo(expected);
        else assertThat(driver.getTitle()).contains(expected);
    }

    @Then("Element with {string} {string} should be {string}")
    public void assertState(String type, String val, String state) {
        WebElement el = getEl(type, val);
        switch(state.toLowerCase()) {
            case "visible" -> assertThat(el.isDisplayed()).isTrue();
            case "hidden" -> assertThat(el.isDisplayed()).isFalse();
            case "enabled" -> assertThat(el.isEnabled()).isTrue();
            case "disabled" -> assertThat(el.isEnabled()).isFalse();
        }
    }

    @Then("Element with {string} {string} text should {string} {string}")
    public void assertText(String type, String val, String condition, String expected) {
        String actual = getEl(type, val).getText();
        if(condition.equals("equal")) assertThat(actual).isEqualTo(expected);
        else assertThat(actual).contains(expected);
    }

    @Then("The list {string} {string} should have {int} items")
    public void listSize(String type, String val, int expectedSize) {
        List<WebElement> elements = driver.findElements(getLocator(type, val));
        assertThat(elements.size()).isEqualTo(expectedSize);
    }

    private WebElement getEl(String type, String val) {
        return wait.until(ExpectedConditions.visibilityOfElementLocated(getLocator(type, val)));
    }

    private By getLocator(String type, String val) {
        return switch (type.toUpperCase()) {
            case "ID" -> By.id(val);
            case "NAME" -> By.name(val);
            case "XPATH" -> By.xpath(val);
            case "CSS" -> By.cssSelector(val);
            case "TEXT" -> By.xpath("//*[text()='" + val + "']");
            default -> throw new IllegalArgumentException("Unsupported selector: " + type);
        };
    }
}