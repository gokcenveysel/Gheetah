package steps;

import io.cucumber.java.en.Given;
import io.cucumber.java.en.When;
import io.cucumber.java.en.Then;
import org.testng.Assert;
import pages.AutocompletePage;
import utils.TestBase;

public class AutocompleteSteps {
    private final AutocompletePage autocompletePage;
    private final TestBase testBase;

    public AutocompleteSteps(TestBase testBase) {
        this.testBase = testBase;
        this.autocompletePage = new AutocompletePage(testBase.WebDriverManager());
    }

    @Given("I am on the Formy autocomplete page")
    public void iAmOnTheFormyAutocompletePage() {
        autocompletePage.navigateTo();
    }

    @When("I enter a valid address in the autocomplete field")
    public void iEnterAValidAddressInTheAutocompleteField() {
        autocompletePage.enterAddress("1555 Park Blvd, Palo Alto, CA, USA");
    }

    @When("I select the first suggested address")
    public void iSelectTheFirstSuggestedAddress() {
        autocompletePage.selectFirstSuggestion();
    }

    @Then("The address fields should be auto-populated")
    public void theAddressFieldsShouldBeAutoPopulated() {
        Assert.assertTrue(autocompletePage.areAddressFieldsPopulated(), "Address fields are not auto-populated");
    }

    @io.cucumber.java.After
    public void tearDown() {
        testBase.tearDown();
    }
}