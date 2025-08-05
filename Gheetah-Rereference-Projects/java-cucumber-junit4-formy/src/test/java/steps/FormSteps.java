package steps;

import io.cucumber.java.en.Given;
import io.cucumber.java.en.When;
import io.cucumber.java.en.Then;
import io.cucumber.java.After;
import pages.FormPage;
import utils.TestBase;
import static org.hamcrest.MatcherAssert.assertThat;
import static org.hamcrest.Matchers.is;

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
        assertThat("Thank you page not displayed", formPage.isThankYouPageDisplayed(), is(true));
    }

    @After
    public void tearDown() {
        testBase.tearDown();
    }
}