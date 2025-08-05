package steps;

import io.cucumber.java.en.Given;
import io.cucumber.java.en.When;
import io.cucumber.java.en.Then;
import io.cucumber.java.After;
import org.testng.Assert;
import pages.FormPage;
import utils.TestBase;

public class FormSteps {
    private final FormPage formPage;
    private final TestBase testBase;

    public FormSteps(TestBase testBase) {
        this.testBase = testBase;
        this.formPage = new FormPage(testBase.WebDriverManager());
    }

    @Given("I navigate to the Formy form page")
    public void iNavigateToFormPage() {
        formPage.navigateTo();
    }

    @When("I fill the form with first name {string}, last name {string}, job title {string}, and date {string}")
    public void iFillTheForm(String firstName, String lastName, String jobTitle, String date) {
        formPage.fillForm(firstName, lastName, jobTitle, date);
    }

    @When("I submit the form")
    public void iSubmitTheForm() {
        formPage.submitForm();
    }

    @Then("I should see the thank you page")
    public void iShouldSeeThankYouPage() {
        Assert.assertTrue(formPage.isThankYouPageDisplayed(), "Thank you page not displayed");
    }

    @After
    public void tearDown() {
        testBase.tearDown();
    }
}