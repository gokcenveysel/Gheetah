using RestSharp;
using Reqnroll;
using FluentAssertions;
using Newtonsoft.Json.Linq;

namespace {{ProjectName}}.StepDefinitions
{
    [Binding]
    public class ApiSteps
    {
        private RestClient _client;
        private RestRequest _request;
        private RestResponse _response;

        [Given(@"I set API Base URL to ""(.*)""")]
        public void SetBase(string url) => _client = new RestClient(url);

        [Given(@"I add header ""(.*)"" with value ""(.*)""")]
        public void AddHeader(string k, string v) => (_request ??= new RestRequest()).AddHeader(k, v);

        [Given(@"I set Bearer Token to ""(.*)""")]
        public void SetToken(string t) => AddHeader("Authorization", $"Bearer {t}");

        [When(@"I send a (GET|POST|PUT|DELETE) request to ""(.*)"" with body:")]
        public void Send(string m, string end, string body)
        {
            _request ??= new RestRequest(end, Enum.Parse<Method>(m));
            _request.Resource = end;
            _request.Method = Enum.Parse<Method>(m);
            if (!string.IsNullOrWhiteSpace(body)) _request.AddJsonBody(body);
            _response = _client.Execute(_request);
        }

        [Then(@"Response status code should be (.*)")]
        public void AssertStatus(int c) => ((int)_response.StatusCode).Should().Be(c);

        [Then(@"Response JSON path ""(.*)"" should (equal|contain) ""(.*)""")]
        public void AssertJson(string path, string opt, string val)
        {
            var token = JObject.Parse(_response.Content).SelectToken(path).ToString();
            if (opt == "equal") token.Should().Be(val); else token.Should().Contain(val);
        }
    }
}