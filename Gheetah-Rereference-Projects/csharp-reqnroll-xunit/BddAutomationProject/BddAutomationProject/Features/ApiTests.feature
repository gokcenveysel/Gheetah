Feature: API Automation Tests
  As a tester
  I want to verify the functionality of API endpoints
  So that I can ensure the API works as expected

Scenario: Perform a valid GET request
  Given I have a valid API endpoint
  When I send a GET request
  Then I should receive a 200 status code

Scenario: Perform a valid POST request
  Given I have a valid API endpoint
  When I send a POST request with valid user data
  Then I should receive a 201 status code

Scenario: Perform a valid PUT request
  Given I have a valid API endpoint with an existing resource
  When I send a PUT request with updated user data
  Then I should receive a 200 status code

Scenario: Perform a valid DELETE request
  Given I have a valid API endpoint with an existing resource
  When I send a DELETE request
  Then I should receive a 200 status code