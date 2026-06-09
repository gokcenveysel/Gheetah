Feature: Gheetah C# Web Automation Showcase

  # ── Navigation & Window ──────────────────────────────────────────────────────
  @Web
  Scenario: Navigation and Window Management
    Given I navigate to URL "https://the-internet.herokuapp.com"
    When I maximize window
    Then Title should contain "The Internet"
    And Current URL should contain "the-internet"
    When I navigate to URL "https://the-internet.herokuapp.com/windows"
    And I click on element with LinkText "Click Here"
    And I switch to tab 1
    Then Title should contain "New Window"
    When I close current tab
    And I switch to main window
    Then Title should contain "Opening a new window"

  # ── Forms & Input ─────────────────────────────────────────────────────────────
  @Web
  Scenario: Form Input and Dropdown Interactions
    Given I navigate to URL "https://the-internet.herokuapp.com/login"
    When I enter "tomsmith" into element with ID "username"
    And I enter "SuperSecretPassword!" into element with ID "password"
    And I click on element with XPath "//button[@type='submit']"
    Then Current URL should contain "secure"
    And Element with ID "flash" text should contain "You logged into a secure area"
    When I navigate to URL "https://the-internet.herokuapp.com/dropdown"
    And I select "Option 1" from dropdown with ID "dropdown"
    Then Dropdown with ID "dropdown" selected option should be "Option 1"
    When I select index 2 from dropdown with ID "dropdown"
    Then Dropdown with ID "dropdown" selected option should be "Option 2"

  # ── Checkboxes & State ────────────────────────────────────────────────────────
  @Web
  Scenario: Checkbox and Element State Assertions
    Given I navigate to URL "https://the-internet.herokuapp.com/checkboxes"
    Then Element with XPath "//input[1]" should be visible
    When I check the checkbox with XPath "//input[1]"
    Then Checkbox with XPath "//input[1]" should be checked
    When I uncheck the checkbox with XPath "//input[2]"
    Then Checkbox with XPath "//input[2]" should be unchecked
    When I check the checkbox with XPath "//input[2]"
    Then Checkbox with XPath "//input[2]" should be checked

  # ── Alerts & Dialogs ──────────────────────────────────────────────────────────
  @Web
  Scenario: Alert and Prompt Handling
    Given I navigate to URL "https://the-internet.herokuapp.com/javascript_alerts"
    When I click on element with XPath "//button[text()='Click for JS Alert']"
    Then Alert text should contain "I am a JS Alert"
    When I accept the alert
    Then Element with ID "result" text should contain "You successfuly"
    When I click on element with XPath "//button[text()='Click for JS Confirm']"
    Then Alert text should contain "I am a JS Confirm"
    When I dismiss the alert
    Then Element with ID "result" text should contain "dismissed"
    When I click on element with XPath "//button[text()='Click for JS Prompt']"
    And I enter "Gheetah Test" in the alert and accept
    Then Element with ID "result" text should contain "Gheetah Test"

  # ── Frames & iFrames ─────────────────────────────────────────────────────────
  @Web
  Scenario: Frame Switching and Interaction
    Given I navigate to URL "https://the-internet.herokuapp.com/iframe"
    When I switch to frame with ID "mce_0_ifr"
    And I click on element with ID "tinymce"
    And I press Ctrl+A key combination
    And I enter "Gheetah Frame Test" into element with ID "tinymce"
    Then Element with ID "tinymce" text should contain "Gheetah Frame Test"
    When I switch to default content

  # ── Scroll & Hover ───────────────────────────────────────────────────────────
  @Web
  Scenario: Scroll and Advanced Mouse Interactions
    Given I navigate to URL "https://the-internet.herokuapp.com/hovers"
    When I hover over element with Css ".figure:first-child"
    Then Element with Css ".figure:first-child .figcaption" should be visible
    When I navigate to URL "https://the-internet.herokuapp.com/infinite_scroll"
    And I scroll to bottom of page
    And I wait 2 seconds
    Then Element count with Css ".jscroll-added p" should be greater than 0

  # ── Waiting Strategies ───────────────────────────────────────────────────────
  @Web
  Scenario: Dynamic Content and Wait Strategies
    Given I navigate to URL "https://the-internet.herokuapp.com/dynamic_loading/1"
    When I click on element with XPath "//button[text()='Start']"
    And I wait for element with ID "finish" to be visible
    Then Element with ID "finish" text should contain "Hello World"

  # ── Element Count & Page Source ──────────────────────────────────────────────
  @Web
  Scenario: Assertions on Lists and Page Source
    Given I navigate to URL "https://the-internet.herokuapp.com/add_remove_elements/"
    When I click on element with XPath "//button[text()='Add Element']"
    And I click on element with XPath "//button[text()='Add Element']"
    And I click on element with XPath "//button[text()='Add Element']"
    Then Element count with Css "#elements button" should be 3
    And Page source should contain "Delete"
    When I click on element with Css "#elements button:last-child"
    Then Element count with Css "#elements button" should be 2

  # ── JavaScript Execution ─────────────────────────────────────────────────────
  @Web
  Scenario: JavaScript Execution and Attribute Manipulation
    Given I navigate to URL "https://the-internet.herokuapp.com/inputs"
    When I click on element with Css "input[type='number']"
    And I execute JavaScript "document.querySelector('input[type=number]').value = '42';"
    Then Element with Css "input[type='number']" attribute "value" should be "42"
    When I highlight element with Css "input[type='number']"
    And I set attribute "data-test" of element with Css "input[type='number']" to "gheetah"
    Then Element with Css "input[type='number']" attribute "data-test" should be "gheetah"

  # ── API Steps ────────────────────────────────────────────────────────────────
  @Api
  Scenario: REST API Authentication and Validation
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
    And Response JSON path "token" should not be empty

  @Api
  Scenario: GET Request and JSON Path Validation
    Given I set API Base URL to "https://reqres.in"
    When I send a GET request to "/api/users/2"
    Then Response status code should be 200
    And Response JSON path "data.first_name" should equal "Janet"
    And Response JSON path "data.last_name" should contain "Weaver"
    And Response JSON path "data.id" should equal "2"

  @Api
  Scenario: POST Request and Resource Creation
    Given I set API Base URL to "https://reqres.in"
    And I add header "Content-Type" with value "application/json"
    When I send a POST request to "/api/users" with body:
    """
    {
      "name": "Gheetah User",
      "job": "QA Automation Engineer"
    }
    """
    Then Response status code should be 201
    And Response JSON path "name" should equal "Gheetah User"
    And Response JSON path "id" should exist
