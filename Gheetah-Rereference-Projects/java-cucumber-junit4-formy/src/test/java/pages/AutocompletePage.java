package pages;

import org.openqa.selenium.WebDriver;
import org.openqa.selenium.WebElement;
import org.openqa.selenium.support.FindBy;
import org.openqa.selenium.support.PageFactory;
import org.openqa.selenium.support.ui.ExpectedConditions;
import org.openqa.selenium.support.ui.WebDriverWait;
import java.time.Duration;

public class AutocompletePage {
    private WebDriver driver;
    private WebDriverWait wait;

    @FindBy(id = "autocomplete")
    private WebElement autocompleteInput;

    @FindBy(className = "pac-item")
    private WebElement firstSuggestion;

    @FindBy(id = "street_number")
    private WebElement streetNumberInput;

    @FindBy(id = "locality")
    private WebElement cityInput;

    @FindBy(id = "administrative_area_level_1")
    private WebElement stateInput;

    @FindBy(id = "postal_code")
    private WebElement zipCodeInput;

    public AutocompletePage(WebDriver driver) {
        this.driver = driver;
        this.wait = new WebDriverWait(driver, Duration.ofSeconds(10));
        PageFactory.initElements(driver, this);
    }

    public void navigateTo() {
        driver.get("https://formy-project.herokuapp.com/autocomplete");
    }

    public void enterAddress(String address) {
        wait.until(ExpectedConditions.visibilityOf(autocompleteInput)).sendKeys(address);
    }

    public void selectFirstSuggestion() {
        wait.until(ExpectedConditions.visibilityOf(firstSuggestion)).click();
    }

    public boolean areAddressFieldsPopulated() {
        String street = wait.until(ExpectedConditions.visibilityOf(streetNumberInput)).getAttribute("value");
        String city = wait.until(ExpectedConditions.visibilityOf(cityInput)).getAttribute("value");
        String state = wait.until(ExpectedConditions.visibilityOf(stateInput)).getAttribute("value");
        String zip = wait.until(ExpectedConditions.visibilityOf(zipCodeInput)).getAttribute("value");
        return !street.isEmpty() && !city.isEmpty() && !state.isEmpty() && !zip.isEmpty();
    }
}