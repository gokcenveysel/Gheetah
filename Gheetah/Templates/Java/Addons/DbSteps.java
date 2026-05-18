package {{ProjectName}}.stepdefinitions;

import io.cucumber.java.en.*;
import java.sql.*;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import static org.assertj.core.api.Assertions.assertThat;

public class DbSteps {
    private Connection connection;
    private ResultSet resultSet;
    private int affectedRows;

    // --- CONNECTION ---
    @Given("I connect to database with connection string {string}")
    public void connectDb(String connectionString) throws SQLException {
        // MSSQL, MySQL veya PostgreSQL fark etmeksizin sürücü üzerinden bağlanır
        connection = DriverManager.getConnection(connectionString);
    }

    @Given("I connect to database with host {string} db {string} user {string} pass {string}")
    public void connectDbParametric(String host, String dbName, String user, String pass) throws SQLException {
        String url = String.format("jdbc:sqlserver://%s;databaseName=%s;encrypt=true;trustServerCertificate=true;", host, dbName);
        connection = DriverManager.getConnection(url, user, pass);
    }

    // --- EXECUTION ---
    @When("I execute query {string}")
    public void executeQuery(String query) throws SQLException {
        Statement statement = connection.createStatement(ResultSet.TYPE_SCROLL_INSENSITIVE, ResultSet.CONCUR_READ_ONLY);
        resultSet = statement.executeQuery(query);
    }

    @When("I execute update-delete-insert query {string}")
    public void executeUpdate(String query) throws SQLException {
        Statement statement = connection.createStatement();
        affectedRows = statement.executeUpdate(query);
    }

    // --- ASSERTIONS & CHECKS ---
    @Then("The database result should contain {string} in column {string}")
    public void verifyValueInColumn(String expectedValue, String columnName) throws SQLException {
        boolean found = false;
        resultSet.beforeFirst(); // ResultSet'i başa al
        while (resultSet.next()) {
            String actualValue = resultSet.getString(columnName);
            if (actualValue != null && actualValue.equals(expectedValue)) {
                found = true;
                break;
            }
        }
        assertThat(found).as("Value [%s] not found in column [%s]", expectedValue, columnName).isTrue();
    }

    @Then("The database result should have {int} rows")
    public void verifyRowCount(int expectedCount) throws SQLException {
        int count = 0;
        resultSet.beforeFirst();
        while (resultSet.next()) {
            count++;
        }
        assertThat(count).as("Database Row Count Check").isEqualTo(expectedCount);
    }

    @Then("The database result should be empty")
    public void verifyEmpty() throws SQLException {
        assertThat(resultSet.next()).as("Database Result Empty Check").isFalse();
    }

    @Then("Affected rows count should be {int}")
    public void verifyAffectedRows(int expectedCount) {
        assertThat(affectedRows).as("Affected Rows Check").isEqualTo(expectedCount);
    }

    @Then("Column {string} at row {int} should be {string}")
    public void verifyValueAtSpecificRow(String columnName, int rowNum, String expectedValue) throws SQLException {
        resultSet.absolute(rowNum);
        String actualValue = resultSet.getString(columnName);
        assertThat(actualValue).as("Value at row %d check", rowNum).isEqualTo(expectedValue);
    }

    // --- UTILS & CLEANUP ---
    @Then("I close the database connection")
    public void closeConnection() throws SQLException {
        if (connection != null && !connection.isClosed()) {
            connection.close();
        }
    }

    @Then("I print the database result")
    public void printResults() throws SQLException {
        ResultSetMetaData rsmd = resultSet.getMetaData();
        int columnsNumber = rsmd.getColumnCount();
        while (resultSet.next()) {
            for (int i = 1; i <= columnsNumber; i++) {
                if (i > 1) System.out.print(",  ");
                String columnValue = resultSet.getString(i);
                System.out.print(rsmd.getColumnName(i) + ": " + columnValue);
            }
            System.out.println("");
        }
    }
}