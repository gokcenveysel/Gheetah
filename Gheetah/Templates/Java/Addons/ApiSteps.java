package {{ProjectName}}.stepdefinitions;

import io.cucumber.java.en.*;
import io.restassured.RestAssured;
import io.restassured.response.Response;
import io.restassured.specification.RequestSpecification;
import io.restassured.http.ContentType;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;
import static org.assertj.core.api.Assertions.assertThat;
import java.util.Map;

public class ApiSteps {
    private RequestSpecification request;
    private Response response;
    private String baseUri;

    // --- SETUP & HEADERS ---
    @Given("I set base URI to {string}")
    public void setBaseUri(String uri) {
        this.baseUri = uri;
        request = RestAssured.given().baseUri(baseUri);
    }

    @Given("I set header {string} to {string}")
    public void setHeader(String key, String value) {
        request.header(key, value);
    }

    @Given("I set content type to {string}")
    public void setContentType(String contentType) {
        request.contentType(contentType);
    }

    @Given("I set bearer token to {string}")
    public void setBearerToken(String token) {
        request.header("Authorization", "Bearer " + token);
    }

    // --- HTTP METHODS ---
    @When("I send a GET request to {string}")
    public void sendGet(String endpoint) {
        response = request.when().get(endpoint);
    }

    @When("I send a POST request to {string} with body:")
    public void sendPost(String endpoint, String body) {
        response = request.body(body).when().post(endpoint);
    }

    @When("I send a PUT request to {string} with body:")
    public void sendPut(String endpoint, String body) {
        response = request.body(body).when().put(endpoint);
    }

    @When("I send a PATCH request to {string} with body:")
    public void sendPatch(String endpoint, String body) {
        response = request.body(body).when().patch(endpoint);
    }

    @When("I send a DELETE request to {string}")
    public void sendDelete(String endpoint) {
        response = request.when().delete(endpoint);
    }

    // --- ASSERTIONS & CHECKS ---
    @Then("The API response status code should be {int}")
    public void verifyStatus(int code) {
        assertThat(response.getStatusCode()).as("Status Code Check").isEqualTo(code);
    }

    @Then("The response time should be less than {long} ms")
    public void verifyResponseTime(long ms) {
        assertThat(response.getTime()).as("Response Time Check").isLessThan(ms);
    }

    @Then("The response body should contain {string}")
    public void verifyBodyContent(String content) {
        assertThat(response.getBody().asString()).contains(content);
    }

    @Then("The response field {string} should be {string}")
    public void verifyField(String jsonPath, String expectedValue) {
        String actualValue = response.jsonPath().getString(jsonPath);
        assertThat(actualValue).as("Field [%s] Check", jsonPath).isEqualTo(expectedValue);
    }

    @Then("The response field {string} should not be null")
    public void verifyFieldNotNull(String jsonPath) {
        assertThat(response.jsonPath().get(jsonPath)).as("Field [%s] Null Check", jsonPath).isNotNull();
    }

    @Then("The response array {string} should have size {int}")
    public void verifyArraySize(String jsonPath, int size) {
        assertThat(response.jsonPath().getList(jsonPath).size()).as("Array Size Check").isEqualTo(size);
    }

    @Then("I print the response body")
    public void printBody() {
        response.prettyPrint();
    }
}