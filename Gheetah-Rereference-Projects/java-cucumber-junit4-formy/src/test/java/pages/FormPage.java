package pages;

import org.openqa.selenium.WebDriver;
import org.openqa.selenium.WebElement;
import org.openqa.selenium.support.FindBy;
import org.openqa.selenium.support.PageFactory;
import org.openqa.selenium.support.ui.ExpectedConditions;
import org.openqa.selenium.support.ui.WebDriverWait;
import java.time.Duration;

public class FormPage {
    private WebDriver driver;
    private WebDriverWait wait;

    @FindBy(id = "first-name")
    private WebElement firstNameInput;

    @FindBy(id = "last-name")
    private WebElement lastNameInput;

    @FindBy(id = "job-title")
    private WebElement jobTitleInput;

    @FindBy(id = "radio-button-2")
    private WebElement collegeRadio;

    @FindBy(id = "checkbox-1")
    private WebElement maleCheckbox;

    @FindBy(id = "select-menu")
    private WebElement experienceSelect;

    @FindBy(id = "datepicker")
    private WebElement dateInput;

    @FindBy(css = "a.btn.btn-lg.btn-primary")
    private WebElement submitButton;

    public FormPage(WebDriver driver) {
        this.driver = driver;
        this.wait = new WebDriverWait(driver, Duration.ofSeconds(10));
        PageFactory.initElements(driver, this);
    }

    public void navigateTo() {
        driver.get("https://formy-project.herokuapp.com/form");
    }

    public void fillForm(String firstName, String lastName, String jobTitle, String date) {
        wait.until(ExpectedConditions.visibilityOf(firstNameInput)).sendKeys(firstName);
        wait.until(ExpectedConditions.visibilityOf(lastNameInput)).sendKeys(lastName);
        wait.until(ExpectedConditions.visibilityOf(jobTitleInput)).sendKeys(jobTitle);
        wait.until(ExpectedConditions.elementToBeClickable(collegeRadio)).click();
        wait.until(ExpectedConditions.elementToBeClickable(maleCheckbox)).click();
        wait.until(ExpectedConditions.elementToBeClickable(experienceSelect)).sendKeys("2-4");
        wait.until(ExpectedConditions.visibilityOf(dateInput)).sendKeys(date);
    }

    public void submitForm() {
        wait.until(ExpectedConditions.elementToBeClickable(submitButton)).click();
    }

    public boolean isThankYouPageDisplayed() {
        return wait.until(ExpectedConditions.urlContains("/thanks"));
    }
}