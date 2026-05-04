using RestSharp;
using Reqnroll;
using FluentAssertions;
using Newtonsoft.Json.Linq;

namespace {{ProjectName}}.StepDefinitions
{
    [Binding]
    public class ApiSteps
    {
        private RestClient? _client;
        private RestRequest? _request;
        private RestResponse? _response;

        [Given(@"I set API Base URL to ""(.*)""")]
        public void SetBaseUrl(string url) => _client = new RestClient(url);

        [Given(@"I add header ""(.*)"" with value ""(.*)""")]
        public void AddHeader(string key, string value) => (_request ??= new RestRequest()).AddHeader(key, value);

        [Given(@"I set Bearer Token to ""(.*)""")]
        public void SetBearerToken(string token) => AddHeader("Authorization", $"Bearer {token}");

        [Given(@"I add query parameter ""(.*)"" with value ""(.*)""")]
        public void AddQueryParameter(string key, string value) => (_request ??= new RestRequest()).AddQueryParameter(key, value);

        [When(@"I send a (GET|POST|PUT|DELETE|PATCH) request to ""(.*)""")]
        public async Task SendRequest(string method, string endpoint)
        {
            if (_client == null) throw new Exception("Base URL not set!");
            
            _request = new RestRequest(endpoint, Enum.Parse<Method>(method));
            _response = await _client.ExecuteAsync(_request);
        }

        [When(@"I send a (POST|PUT|PATCH) request to ""(.*)"" with body:")]
        public async Task SendRequestWithBody(string method, string endpoint, string body)
        {
            if (_client == null) throw new Exception("Base URL not set!");

            _request = new RestRequest(endpoint, Enum.Parse<Method>(method));
            _request.AddJsonBody(body);
            _response = await _client.ExecuteAsync(_request);
        }

        [When(@"I upload file ""(.*)"" to ""(.*)"" with key ""(.*)""")]
        public async Task UploadFile(string filePath, string endpoint, string key)
        {
            _request = new RestRequest(endpoint, Method.Post);
            _request.AddFile(key, filePath);
            _response = await _client!.ExecuteAsync(_request);
        }

        [Then(@"Response status code should be (.*)")]
        public void AssertStatusCode(int expectedCode)
        {
            _response.Should().NotBeNull("No response received. Did you send the request?");
            ((int)_response!.StatusCode).Should().Be(expectedCode);
        }

        [Then(@"Response JSON path ""(.*)"" should (equal|contain) ""(.*)""")]
        public void AssertJsonPath(string path, string option, string expected)
        {
            var content = _response?.Content ?? throw new Exception("Response content is empty!");
            var token = JObject.Parse(content).SelectToken(path);
            
            token.Should().NotBeNull($"JSON path '{path}' not found.");
            
            string actual = token!.ToString();
            if (option == "equal") actual.Should().Be(expected);
            else actual.Should().Contain(expected);
        }

        [Then(@"Response should contain header ""(.*)"" with value ""(.*)""")]
        public void AssertResponseHeader(string header, string value)
        {
            _response!.Headers.Should().Contain(h => h.Name == header && h.Value.ToString() == value);
        }
    }
}