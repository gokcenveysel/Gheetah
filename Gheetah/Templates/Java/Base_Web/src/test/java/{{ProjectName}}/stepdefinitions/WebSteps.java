package {{ProjectName}}.stepdefinitions;

import {{ProjectName}}.drivers.DriverFactory;
import io.cucumber.java.en.*;
import org.openqa.selenium.*;
import org.openqa.selenium.interactions.Actions;
import org.openqa.selenium.support.ui.ExpectedConditions;
import org.openqa.selenium.support.ui.Select;
import org.openqa.selenium.support.ui.WebDriverWait;
import static org.assertj.core.api.Assertions.assertThat;
import java.awt.Dimension;
import java.time.Duration;
import java.util.List;
import java.util.Set;
import java.util.ArrayList;

public class WebSteps {
    private final WebDriver driver = DriverFactory.getDriver();
    private final WebDriverWait wait = new WebDriverWait(driver, Duration.ofSeconds(20));

    // ── Navigation ────────────────────────────────────────────
    @Given("I navigate to URL {string}")
    public void navigateTo(String url) { driver.get(url); }

    @When("I maximize window")
    public void maximizeWindow() { driver.manage().window().maximize(); }

    @When("I minimize window")
    public void minimizeWindow() { driver.manage().window().minimize(); }

    @When("I refresh the page")
    public void refresh() { driver.navigate().refresh(); }

    @When("I go back")
    public void goBack() { driver.navigate().back(); }

    @When("I go forward")
    public void goForward() { driver.navigate().forward(); }

    @When("I set window size to {int}x{int}")
    public void setWindowSize(int width, int height) {
        driver.manage().window().setSize(new org.openqa.selenium.Dimension(width, height));
    }

    // ── Click Operations ─────────────────────────────────────
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

    @When("I right click on element with {string} {string}")
    public void rightClick(String type, String val) {
        new Actions(driver).contextClick(getEl(type, val)).perform();
    }

    @When("I click at coordinates {int}, {int}")
    public void clickAtCoordinates(int x, int y) {
        new Actions(driver).moveByOffset(x, y).click().perform();
    }

    // ── Input Operations ─────────────────────────────────────
    @When("I enter {string} into element with {string} {string}")
    public void enterText(String text, String type, String val) {
        WebElement el = getEl(type, val);
        el.clear();
        el.sendKeys(text);
    }

    @When("I append {string} to element with {string} {string}")
    public void appendText(String text, String type, String val) {
        getEl(type, val).sendKeys(text);
    }

    @When("I clear element with {string} {string}")
    public void clearElement(String type, String val) { getEl(type, val).clear(); }

    @When("I type slowly {string} into element with {string} {string}")
    public void typeSlowly(String text, String type, String val) throws InterruptedException {
        WebElement el = getEl(type, val);
        el.clear();
        for (char c : text.toCharArray()) {
            el.sendKeys(String.valueOf(c));
            Thread.sleep(50);
        }
    }

    @When("I upload file {string} to element with {string} {string}")
    public void uploadFile(String filePath, String type, String val) {
        getEl(type, val).sendKeys(filePath);
    }

    // ── Dropdown / Select ─────────────────────────────────────
    @When("I select {string} from dropdown {string} {string}")
    public void selectByText(String text, String type, String val) {
        new Select(getEl(type, val)).selectByVisibleText(text);
    }

    @When("I select value {string} from dropdown {string} {string}")
    public void selectByValue(String value, String type, String val) {
        new Select(getEl(type, val)).selectByValue(value);
    }

    @When("I select index {int} from dropdown {string} {string}")
    public void selectByIndex(int index, String type, String val) {
        new Select(getEl(type, val)).selectByIndex(index);
    }

    // ── Checkbox / Radio ──────────────────────────────────────
    @When("I check the {string} with {string} {string}")
    public void checkElement(String kind, String type, String val) {
        WebElement el = getEl(type, val);
        if (!el.isSelected()) el.click();
    }

    @When("I uncheck the checkbox with {string} {string}")
    public void uncheckElement(String type, String val) {
        WebElement el = getEl(type, val);
        if (el.isSelected()) el.click();
    }

    // ── Keyboard ─────────────────────────────────────────────
    @When("I press {string} key")
    public void pressKey(String key) {
        Keys k = switch (key) {
            case "Enter" -> Keys.ENTER; case "Tab" -> Keys.TAB;
            case "Escape" -> Keys.ESCAPE; case "Space" -> Keys.SPACE;
            case "Backspace" -> Keys.BACK_SPACE; case "Delete" -> Keys.DELETE;
            case "F1" -> Keys.F1; case "F2" -> Keys.F2; case "F5" -> Keys.F5;
            case "ArrowUp" -> Keys.ARROW_UP; case "ArrowDown" -> Keys.ARROW_DOWN;
            case "ArrowLeft" -> Keys.ARROW_LEFT; case "ArrowRight" -> Keys.ARROW_RIGHT;
            case "Home" -> Keys.HOME; case "End" -> Keys.END;
            case "PageUp" -> Keys.PAGE_UP; case "PageDown" -> Keys.PAGE_DOWN;
            default -> Keys.ENTER;
        };
        new Actions(driver).sendKeys(k).perform();
    }

    @When("I press {string}+{string} key combination")
    public void pressKeyCombination(String modifier, String key) {
        Keys mod = switch (modifier) {
            case "Ctrl" -> Keys.CONTROL; case "Alt" -> Keys.ALT; default -> Keys.SHIFT;
        };
        new Actions(driver).keyDown(mod).sendKeys(key.toLowerCase()).keyUp(mod).perform();
    }

    @When("I press key on element with {string} {string}: {string}")
    public void pressKeyOnElement(String type, String val, String key) {
        Keys k = switch (key) {
            case "Enter" -> Keys.ENTER; case "Tab" -> Keys.TAB;
            case "Escape" -> Keys.ESCAPE; case "Backspace" -> Keys.BACK_SPACE;
            default -> Keys.DELETE;
        };
        getEl(type, val).sendKeys(k);
    }

    // ── Mouse / Advanced Interactions ────────────────────────
    @When("I hover over element with {string} {string}")
    public void hover(String type, String val) {
        new Actions(driver).moveToElement(getEl(type, val)).perform();
    }

    @When("I scroll to element with {string} {string}")
    public void scrollToElement(String type, String val) {
        ((JavascriptExecutor) driver).executeScript(
            "arguments[0].scrollIntoView({block:'center'});", getEl(type, val));
    }

    @When("I scroll to {string} of page")
    public void scrollPage(String position) {
        String script = position.equals("top") ? "window.scrollTo(0,0);"
            : "window.scrollTo(0, document.body.scrollHeight);";
        ((JavascriptExecutor) driver).executeScript(script);
    }

    @When("I scroll down {int} pixels")
    public void scrollByPixels(int pixels) {
        ((JavascriptExecutor) driver).executeScript("window.scrollBy(0," + pixels + ");");
    }

    @When("I drag element with {string} {string} and drop to element with {string} {string}")
    public void dragAndDrop(String srcType, String srcVal, String tgtType, String tgtVal) {
        new Actions(driver).dragAndDrop(getEl(srcType, srcVal), getEl(tgtType, tgtVal)).perform();
    }

    // ── Alerts / Dialogs ──────────────────────────────────────
    @When("I accept the alert")
    public void acceptAlert() {
        wait.until(ExpectedConditions.alertIsPresent()).accept();
    }

    @When("I dismiss the alert")
    public void dismissAlert() {
        wait.until(ExpectedConditions.alertIsPresent()).dismiss();
    }

    @When("I enter {string} in the alert and accept")
    public void enterTextInAlert(String text) {
        Alert alert = wait.until(ExpectedConditions.alertIsPresent());
        alert.sendKeys(text);
        alert.accept();
    }

    @Then("Alert text should {string} {string}")
    public void assertAlertText(String condition, String expected) {
        String alertText = wait.until(ExpectedConditions.alertIsPresent()).getText();
        if (condition.equals("be")) assertThat(alertText).isEqualTo(expected);
        else assertThat(alertText).contains(expected);
    }

    // ── Frames / iFrames ──────────────────────────────────────
    @When("I switch to frame with {string} {string}")
    public void switchToFrame(String type, String val) {
        WebElement frame = type.equals("XPATH") ? driver.findElement(By.xpath(val))
            : driver.findElement(By.id(val));
        driver.switchTo().frame(frame);
    }

    @When("I switch to frame index {int}")
    public void switchToFrameIndex(int index) { driver.switchTo().frame(index); }

    @When("I switch to default content")
    public void switchDefault() { driver.switchTo().defaultContent(); }

    @When("I switch to parent frame")
    public void switchParentFrame() { driver.switchTo().parentFrame(); }

    // ── Windows / Tabs ────────────────────────────────────────
    @When("I open new tab")
    public void openNewTab() {
        ((JavascriptExecutor) driver).executeScript("window.open('','_blank');");
    }

    @When("I switch to tab {int}")
    public void switchToTab(int index) {
        List<String> handles = new ArrayList<>(driver.getWindowHandles());
        driver.switchTo().window(handles.get(index));
    }

    @When("I close current tab")
    public void closeCurrentTab() { driver.close(); }

    @When("I switch to main window")
    public void switchToMainWindow() {
        List<String> handles = new ArrayList<>(driver.getWindowHandles());
        driver.switchTo().window(handles.get(0));
    }

    // ── Wait ──────────────────────────────────────────────────
    @When("I wait {int} seconds")
    public void waitSeconds(int seconds) throws InterruptedException {
        Thread.sleep(seconds * 1000L);
    }

    @When("I wait {int} milliseconds")
    public void waitMilliseconds(int ms) throws InterruptedException {
        Thread.sleep(ms);
    }

    @When("I wait for element with {string} {string} to be visible")
    public void waitForElementVisible(String type, String val) {
        wait.until(ExpectedConditions.visibilityOfElementLocated(getLocator(type, val)));
    }

    @When("I wait for element with {string} {string} to disappear")
    public void waitForElementInvisible(String type, String val) {
        wait.until(ExpectedConditions.invisibilityOfElementLocated(getLocator(type, val)));
    }

    @When("I wait for URL to contain {string}")
    public void waitForUrl(String partial) {
        wait.until(ExpectedConditions.urlContains(partial));
    }

    @When("I wait for page title to contain {string}")
    public void waitForTitle(String partial) {
        wait.until(ExpectedConditions.titleContains(partial));
    }

    // ── JavaScript Execution ──────────────────────────────────
    @When("I execute JavaScript {string}")
    public void executeJS(String script) {
        ((JavascriptExecutor) driver).executeScript(script);
    }

    @When("I set attribute {string} of element with {string} {string} to {string}")
    public void setAttribute(String attr, String type, String val, String attrValue) {
        ((JavascriptExecutor) driver).executeScript(
            "arguments[0].setAttribute(arguments[1], arguments[2]);",
            getEl(type, val), attr, attrValue);
    }

    @When("I highlight element with {string} {string}")
    public void highlightElement(String type, String val) {
        ((JavascriptExecutor) driver).executeScript(
            "arguments[0].style.border='3px solid red';", getEl(type, val));
    }

    // ── Cookies ───────────────────────────────────────────────
    @When("I add cookie with name {string} and value {string}")
    public void addCookie(String name, String value) {
        driver.manage().addCookie(new Cookie(name, value));
    }

    @When("I delete cookie with name {string}")
    public void deleteCookie(String name) {
        driver.manage().deleteCookieNamed(name);
    }

    @When("I delete all cookies")
    public void deleteAllCookies() { driver.manage().deleteAllCookies(); }

    // ── Assertions ───────────────────────────────────────────
    @Then("Page title should {string} {string}")
    public void assertTitle(String condition, String expected) {
        if (condition.equals("be")) assertThat(driver.getTitle()).isEqualTo(expected);
        else assertThat(driver.getTitle()).contains(expected);
    }

    @Then("Current URL should {string} {string}")
    public void assertUrl(String condition, String expected) {
        switch (condition) {
            case "be" -> assertThat(driver.getCurrentUrl()).isEqualTo(expected);
            case "contain" -> assertThat(driver.getCurrentUrl()).contains(expected);
            case "end with" -> assertThat(driver.getCurrentUrl()).endsWith(expected);
        }
    }

    @Then("Page source should {string} {string}")
    public void assertPageSource(String condition, String text) {
        if (condition.equals("contain")) assertThat(driver.getPageSource()).contains(text);
        else assertThat(driver.getPageSource()).doesNotContain(text);
    }

    @Then("Element with {string} {string} should be {string}")
    public void assertState(String type, String val, String state) {
        WebElement el = getEl(type, val);
        switch (state.toLowerCase()) {
            case "visible"  -> assertThat(el.isDisplayed()).isTrue();
            case "hidden"   -> assertThat(el.isDisplayed()).isFalse();
            case "enabled"  -> assertThat(el.isEnabled()).isTrue();
            case "disabled" -> assertThat(el.isEnabled()).isFalse();
        }
    }

    @Then("Element with {string} {string} text should {string} {string}")
    public void assertText(String type, String val, String condition, String expected) {
        String actual = getEl(type, val).getText().trim();
        switch (condition) {
            case "equal"      -> assertThat(actual).isEqualTo(expected);
            case "contain"    -> assertThat(actual).contains(expected);
            case "start with" -> assertThat(actual).startsWith(expected);
            case "end with"   -> assertThat(actual).endsWith(expected);
        }
    }

    @Then("Element with {string} {string} attribute {string} should {string} {string}")
    public void assertAttribute(String type, String val, String attr, String condition, String expected) {
        String actual = getEl(type, val).getAttribute(attr);
        if (condition.equals("be")) assertThat(actual).isEqualTo(expected);
        else assertThat(actual).contains(expected);
    }

    @Then("Element count with {string} {string} should be {int}")
    public void listSize(String type, String val, int expectedSize) {
        assertThat(driver.findElements(getLocator(type, val))).hasSize(expectedSize);
    }

    @Then("Element count with {string} {string} should be greater than {int}")
    public void listSizeGreaterThan(String type, String val, int min) {
        assertThat(driver.findElements(getLocator(type, val)).size()).isGreaterThan(min);
    }

    @Then("Dropdown with {string} {string} selected option should {string} {string}")
    public void assertDropdownSelection(String type, String val, String condition, String expected) {
        String selected = new Select(getEl(type, val)).getFirstSelectedOption().getText();
        if (condition.equals("be")) assertThat(selected).isEqualTo(expected);
        else assertThat(selected).contains(expected);
    }

    @Then("Checkbox with {string} {string} should be {string}")
    public void assertCheckboxState(String type, String val, String state) {
        boolean isChecked = getEl(type, val).isSelected();
        if (state.equals("checked")) assertThat(isChecked).isTrue();
        else assertThat(isChecked).isFalse();
    }

    @Then("Cookie {string} should exist")
    public void assertCookieExists(String name) {
        assertThat(driver.manage().getCookieNamed(name)).isNotNull();
    }

    // ── Helpers ───────────────────────────────────────────────
    private WebElement getEl(String type, String val) {
        return wait.until(ExpectedConditions.visibilityOfElementLocated(getLocator(type, val)));
    }

    private By getLocator(String type, String val) {
        return switch (type.toUpperCase()) {
            case "ID"       -> By.id(val);
            case "NAME"     -> By.name(val);
            case "XPATH"    -> By.xpath(val);
            case "CSS"      -> By.cssSelector(val);
            case "TEXT"     -> By.xpath("//*[normalize-space(text())='" + val + "']");
            case "LINKTEXT" -> By.linkText(val);
            case "TAG"      -> By.tagName(val);
            default -> throw new IllegalArgumentException("Unsupported selector: " + type);
        };
    }
}
