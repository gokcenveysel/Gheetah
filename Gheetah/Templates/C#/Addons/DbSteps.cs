using Reqnroll;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Dapper;

namespace {{ProjectName}}.StepDefinitions
{
    [Binding]
    public class DbSteps
    {
        private string? _conn;

        [Given(@"I connect to SQL Server: ""(.*)""")]
        public void Connect(string c) => _conn = c;

        [When(@"I execute SQL: ""(.*)""")]
        public void Exec(string sql) { using var c = new SqlConnection(_conn); c.Execute(sql); }

        [Then(@"DB query ""(.*)"" result at row (.*) and column ""(.*)"" should be ""(.*)""")]
        public void AssertVal(string sql, int row, string col, string exp)
        {
            using var c = new SqlConnection(_conn);
            var res = c.Query(sql).ToList()[row - 1] as IDictionary<string, object>;
            res[col].ToString().Should().Be(exp);
        }
    }
}