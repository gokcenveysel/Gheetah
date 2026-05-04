using Reqnroll;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Data;

namespace {{ProjectName}}.StepDefinitions
{
    [Binding]
    public class DbSteps
    {
        private string? _connectionString;

        [Given(@"I connect to SQL Server: ""(.*)""")]
        public void ConnectToDatabase(string connectionString) => _connectionString = connectionString;

        [When(@"I execute SQL: ""(.*)""")]
        public void ExecuteSql(string sql)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Execute(sql);
        }

        [When(@"I execute SQL with parameters:")]
        public void ExecuteSqlWithParams(string sql, Table table)
        {
            var parameters = table.Rows.ToDictionary(r => r["Parameter"], r => r["Value"] as object);
            using var connection = new SqlConnection(_connectionString);
            connection.Execute(sql, parameters);
        }

        [Then(@"DB query ""(.*)"" result at row (.*) and column ""(.*)"" should be ""(.*)""")]
        public void AssertDbValue(string sql, int row, string column, string expected)
        {
            using var connection = new SqlConnection(_connectionString);
            var results = connection.Query(sql).ToList();
            
            results.Should().NotBeEmpty("Query returned no results.");
            row = row - 1; // 1-based to 0-based
            results[row].Should().NotBeNull();

            var value = ((IDictionary<string, object>)results[row])[column]?.ToString();
            value.Should().Be(expected);
        }

        [Then(@"DB query ""(.*)"" should return (.*) rows")]
        public void AssertRowCount(string sql, int expectedCount)
        {
            using var connection = new SqlConnection(_connectionString);
            var count = connection.Query<int>($"SELECT COUNT(*) FROM ({sql}) AS sub").Single();
            count.Should().Be(expectedCount);
        }

        [Then(@"DB query ""(.*)"" result should match:")]
        public void AssertQueryResult(string sql, Table table)
        {
            using var connection = new SqlConnection(_connectionString);
            var results = connection.Query(sql).ToList();

            results.Should().NotBeEmpty();

            for (int i = 0; i < table.RowCount; i++)
            {
                var expectedRow = table.Rows[i];
                var actualRow = (IDictionary<string, object>)results[i];

                foreach (var header in table.Header)
                {
                    actualRow[header].ToString().Should().Be(expectedRow[header]);
                }
            }
        }
    }
}