Feature: Gheetah Full Potential Showcase

  @Web
  Scenario: Complex UI Interaction
    Given I navigate to URL "https://automationexercise.com"
    When I maximize window
    And I click on element with Text "Signup / Login"
    And I enter "Gheetah User" into element with Name "name"
    And I enter "test@gheetah.com" into element with XPath "(//input[@name='email'])[2]"
    And I click on element with XPath "//button[text()='Signup']"
    Then Element with ID "id_gender1" should be visible

  @Api
  Scenario: API Authentication and Data Check
    Given I set API Base URL to "https://api.test.com"
    And I add header "Content-Type" with value "application/json"
    When I send a POST request to "/api/login" with body:
    """
    { "user": "admin", "pass": "1234" }
    """
    Then Response status code should be 200
    And Response JSON path "data.role" should equal "admin"