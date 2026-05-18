Feature: Gheetah Full Potential Showcase

  @Web
  Scenario: Complex Web UI Interactions
    Given I navigate to URL "https://automationexercise.com"
    When I maximize window
    And I click on element with Text "Signup / Login"
    And I enter "Gheetah User" into element with Name "name"
    And I enter "test@gheetah.com" into element with XPath "(//input[@name='email'])[2]"
    And I click on element with XPath "//button[text()='Signup']"
    Then Element with ID "id_gender1" should be visible
    And Element with ID "id_gender1" should be enabled
    When I click on element with ID "id_gender1"
    And I enter "Gheetah Tester" into element with ID "name"
    And I scroll to element with XPath "//button[text()='Create Account']"
    And I hover over element with Text "Create Account"
    Then Element with Text "Create Account" should be visible

  @Api
  Scenario: API Authentication and Data Validation
    Given I set API Base URL to "https://reqres.in"
    And I add header "Content-Type" with value "application/json"
    When I send a POST request to "/api/login" with body:
    """
    {
      "email": "eve.holt@reqres.in",
      "password": "cityslicka"
    }
    """
    Then Response status code should be 200
    And Response JSON path "token" should exist
    And Response JSON path "token" should contain "Qpw"

  @Api
  Scenario: GET Request and JSON Path Validation
    Given I set API Base URL to "https://reqres.in"
    When I send a GET request to "/api/users/2"
    Then Response status code should be 200
    And Response JSON path "data.first_name" should equal "Janet"
    And Response JSON path "data.last_name" should contain "Weaver"