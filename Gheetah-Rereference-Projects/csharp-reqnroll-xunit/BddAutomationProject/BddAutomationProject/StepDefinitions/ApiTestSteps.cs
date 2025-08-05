using BddAutomationProject.Models;
using Reqnroll;
using RestSharp;
using Xunit;

namespace BddAutomationProject.StepDefinitions
{
    [Binding]
    public class ApiTestSteps
    {
        private readonly ScenarioContext _scenarioContext;
        private RestClient _client;
        private RestResponse _response;

        public ApiTestSteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
            _client = new RestClient("https://jsonplaceholder.typicode.com");
        }

        [Given(@"I have a valid API endpoint")]
        public void GivenIHaveAValidAPIEndpoint()
        {
            // Endpoint already set in RestClient
        }

        [Given(@"I have a valid API endpoint with an existing resource")]
        public void GivenIHaveAValidAPIEndpointWithAnExistingResource()
        {
            // Using a known resource ID for testing
            _scenarioContext["resourceId"] = 1;
        }

        [When(@"I send a GET request")]
        public void WhenISendAGETRequest()
        {
            var request = new RestRequest("/posts/1", Method.Get);
            _response = _client.Execute(request);
        }

        [When(@"I send a POST request with valid user data")]
        public void WhenISendAPOSTRequestWithValidUserData()
        {
            var request = new RestRequest("/posts", Method.Post);
            request.AddJsonBody(new User
            {
                Title = "Test Post",
                Body = "This is a test post",
                UserId = 1
            });
            _response = _client.Execute(request);
        }

        [When(@"I send a PUT request with updated user data")]
        public void WhenISendAPUTRequestWithUpdatedUserData()
        {
            var request = new RestRequest("/posts/1", Method.Put);
            request.AddJsonBody(new User
            {
                Id = 1,
                Title = "Updated Post",
                Body = "This is an updated test post",
                UserId = 1
            });
            _response = _client.Execute(request);
        }

        [When(@"I send a DELETE request")]
        public void WhenISendADELETERequest()
        {
            var request = new RestRequest("/posts/1", Method.Delete);
            _response = _client.Execute(request);
        }

        [Then(@"I should receive a (.*) status code")]
        public void ThenIShouldReceiveAStatusCode(int expectedStatusCode)
        {
            Assert.Equal(expectedStatusCode, (int)_response.StatusCode);
        }
    }
}