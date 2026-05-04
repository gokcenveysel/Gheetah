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
        public void SetBase(string url) => _client = new RestClient(url);

        [Given(@"I add header ""(.*)"" with value ""(.*)""")]
        public void AddHeader(string k, string v) => (_request ??= new RestRequest()).AddHeader(k, v);

        [Given(@"I set Bearer Token to ""(.*)""")]
        public void SetToken(string t) => AddHeader("Authorization", $"Bearer {t}");

        [When(@"I send a (GET|POST|PUT|DELETE) request to ""(.*)"" with body:")]
        public async Task Send(string m, string end, string body) 
        {
            if (_client == null) throw new Exception("Base URL not set!");

            _request ??= new RestRequest(end);
            _request.Method = Enum.Parse<Method>(m);
            _request.Resource = end;

            if (!string.IsNullOrWhiteSpace(body)) 
                _request.AddJsonBody(body);
            
            _response = await _client.ExecuteAsync(_request); 
        }

        [Then(@"Response status code should be (.*)")]
        public void AssertStatus(int c) 
        {
            _response.Should().NotBeNull("Response must not be null. Did you send the request?");
            ((int)_response!.StatusCode).Should().Be(c);
        }

        [Then(@"Response JSON path ""(.*)"" should (equal|contain) ""(.*)""")]
        public void AssertJson(string path, string opt, string val)
        {
            var content = _response?.Content ?? throw new Exception("Response content is empty!");
            var token = JObject.Parse(content).SelectToken(path);
            
            token.Should().NotBeNull($"Path '{path}' not found in JSON response.");
            
            if (opt == "equal") token!.ToString().Should().Be(val); 
            else token!.ToString().Should().Contain(val);
        }
    }
}