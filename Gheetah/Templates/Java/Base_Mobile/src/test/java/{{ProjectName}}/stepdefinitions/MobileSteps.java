package {{ProjectName}}.stepdefinitions;

import {{ProjectName}}.drivers.DriverFactory;
import io.appium.java_client.AppiumBy;
import io.appium.java_client.AppiumDriver;
import io.cucumber.java.en.*;
import org.openqa.selenium.WebElement;
import org.openqa.selenium.support.ui.ExpectedConditions;
import org.openqa.selenium.support.ui.WebDriverWait;
import static org.assertj.core.api.Assertions.assertThat;
import java.time.Duration;

public class MobileSteps {
    private final AppiumDriver driver = DriverFactory.getDriver();
    private final WebDriverWait wait = new WebDriverWait(driver, Duration.ofSeconds(20));

    @Given("I launch {string} application {string} on device {string}")
    public void launchApp(String platform, String app, String device) throws Exception {
        // DriverFactory üzerinden dinamik başlatma
        DriverFactory.createDriver(platform, app, device);
    }

    @When("I tap on mobile element with {string} {string}")
    public void tapElement(String type, String val) { getEl(type, val).click(); }

    @When("I enter {string} into mobile element with {string} {string}")
    public void enterText(String text, String type, String val) {
        WebElement el = getEl(type, val);
        el.clear();
        el.sendKeys(text);
    }

    @When("I hide keyboard")
    public void hideKeyboard() {
        if (driver instanceof io.appium.java_client.HidesKeyboard) {
            ((io.appium.java_client.HidesKeyboard) driver).hideKeyboard();
        }
    }

    @Then("Mobile element with {string} {string} should be visible")
    public void isVisible(String type, String val) {
        assertThat(getEl(type, val).isDisplayed()).isTrue();
    }

    @Then("Mobile element with {string} {string} text should be {string}")
    public void verifyText(String type, String val, String expectedText) {
        assertThat(getEl(type, val).getText()).isEqualTo(expectedText);
    }

    // Helper: Appium için optimize edilmiş element bulucu
    private WebElement getEl(String type, String val) {
        org.openqa.selenium.By by = switch (type.toUpperCase()) {
            case "ID" -> AppiumBy.id(val);
            case "ACCESSIBILITYID" -> AppiumBy.accessibilityId(val);
            case "XPATH" -> AppiumBy.xpath(val);
            case "TEXT" -> AppiumBy.androidUIAutomator("new UiSelector().text(\"" + val + "\")");
            case "IOSCLASSCHAIN" -> AppiumBy.iOSClassChain(val);
            default -> AppiumBy.id(val);
        };
        return wait.until(ExpectedConditions.visibilityOfElementLocated(by));
    }
}