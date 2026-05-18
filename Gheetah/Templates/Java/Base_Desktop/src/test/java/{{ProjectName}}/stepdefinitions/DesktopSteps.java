package {{ProjectName}}.stepdefinitions;

import {{ProjectName}}.drivers.DriverFactory;
import io.appium.java_client.AppiumBy;
import io.appium.java_client.windows.WindowsDriver;
import io.cucumber.java.en.*;
import org.openqa.selenium.WebElement;
import org.openqa.selenium.interactions.Actions;
import org.openqa.selenium.support.ui.ExpectedConditions;
import org.openqa.selenium.support.ui.WebDriverWait;
import static org.assertj.core.api.Assertions.assertThat;
import java.time.Duration;

public class DesktopSteps {
    private WindowsDriver driver;
    private WebDriverWait wait;

    @Given("I launch desktop application at {string}")
    public void launchApp(String appPath) throws Exception {
        driver = DriverFactory.createDriver(appPath);
        wait = new WebDriverWait(driver, Duration.ofSeconds(15));
    }

    @When("I click on desktop element with {string} {string}")
    public void clickElement(String type, String val) {
        getEl(type, val).click();
    }

    @When("I type {string} into desktop element with {string} {string}")
    public void typeText(String text, String type, String val) {
        WebElement el = getEl(type, val);
        el.clear();
        el.sendKeys(text);
    }

    @When("I double click on desktop element with {string} {string}")
    public void doubleClick(String type, String val) {
        new Actions(driver).doubleClick(getEl(type, val)).perform();
    }

    @When("I maximize the desktop window")
    public void maximizeWindow() {
        driver.manage().window().maximize();
    }

    @Then("Desktop element with {string} {string} should exist")
    public void elementExists(String type, String val) {
        assertThat(getEl(type, val).isDisplayed()).isTrue();
    }

    @Then("Desktop element with {string} {string} text should be {string}")
    public void verifyText(String type, String val, String expected) {
        assertThat(getEl(type, val).getText()).isEqualTo(expected);
    }

    // Helper: Windows UI Automation Seçicileri
    private WebElement getEl(String type, String val) {
        org.openqa.selenium.By by = switch (type.toUpperCase()) {
            case "NAME" -> AppiumBy.name(val);
            case "ID" -> AppiumBy.accessibilityId(val);
            case "XPATH" -> AppiumBy.xpath(val);
            case "CLASS" -> AppiumBy.className(val);
            default -> AppiumBy.accessibilityId(val);
        };
        return wait.until(ExpectedConditions.visibilityOfElementLocated(by));
    }
}