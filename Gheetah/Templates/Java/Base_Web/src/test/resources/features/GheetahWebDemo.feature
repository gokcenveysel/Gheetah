@Web
Feature: Gheetah Java Web Automation Showcase

  # ── Navigation & Window ──────────────────────────────────────────────────────
  Scenario: Navigation and URL Assertions
    Given I navigate to URL "https://the-internet.herokuapp.com"
    When I maximize window
    Then Page title should "contain" "The Internet"
    And Current URL should "contain" "the-internet"
    When I refresh the page
    Then Page title should "contain" "The Internet"

  # ── Login & Form Input ────────────────────────────────────────────────────────
  Scenario: Login Form with Input and Assertions
    Given I navigate to URL "https://the-internet.herokuapp.com/login"
    When I enter "tomsmith" into element with "ID" "username"
    And I enter "SuperSecretPassword!" into element with "ID" "password"
    And I click on element with "XPATH" "//button[@type='submit']"
    Then Current URL should "contain" "secure"
    And Element with "ID" "flash" text should "contain" "You logged into a secure area"

  # ── Dropdown ─────────────────────────────────────────────────────────────────
  Scenario: Dropdown Selection and Validation
    Given I navigate to URL "https://the-internet.herokuapp.com/dropdown"
    When I select "Option 1" from dropdown "ID" "dropdown"
    Then Dropdown with "ID" "dropdown" selected option should "be" "Option 1"
    When I select index 2 from dropdown "ID" "dropdown"
    Then Dropdown with "ID" "dropdown" selected option should "be" "Option 2"

  # ── Checkboxes ───────────────────────────────────────────────────────────────
  Scenario: Checkbox State Management
    Given I navigate to URL "https://the-internet.herokuapp.com/checkboxes"
    When I check the "checkbox" with "XPATH" "//input[1]"
    Then Checkbox with "XPATH" "//input[1]" should be "checked"
    When I uncheck the checkbox with "XPATH" "//input[2]"
    Then Checkbox with "XPATH" "//input[2]" should be "unchecked"

  # ── Alerts / Dialogs ──────────────────────────────────────────────────────────
  Scenario: Alert and Confirm Dialog Handling
    Given I navigate to URL "https://the-internet.herokuapp.com/javascript_alerts"
    When I click on element with "XPATH" "//button[text()='Click for JS Alert']"
    Then Alert text should "contain" "I am a JS Alert"
    When I accept the alert
    Then Element with "ID" "result" text should "contain" "You successfuly"
    When I click on element with "XPATH" "//button[text()='Click for JS Confirm']"
    And I dismiss the alert
    Then Element with "ID" "result" text should "contain" "dismissed"
    When I click on element with "XPATH" "//button[text()='Click for JS Prompt']"
    And I enter "Gheetah Java" in the alert and accept
    Then Element with "ID" "result" text should "contain" "Gheetah Java"

  # ── Hover & Scroll ────────────────────────────────────────────────────────────
  Scenario: Mouse Hover and Scroll Interactions
    Given I navigate to URL "https://the-internet.herokuapp.com/hovers"
    When I hover over element with "CSS" ".figure:first-child"
    Then Element with "CSS" ".figure:first-child .figcaption" should be "visible"
    When I navigate to URL "https://the-internet.herokuapp.com/large"
    And I scroll to "bottom" of page
    And I wait 1 seconds
    And I scroll to "top" of page

  # ── Wait Strategies ──────────────────────────────────────────────────────────
  Scenario: Dynamic Content Loading with Explicit Waits
    Given I navigate to URL "https://the-internet.herokuapp.com/dynamic_loading/1"
    When I click on element with "XPATH" "//button[text()='Start']"
    And I wait for element with "ID" "finish" to be visible
    Then Element with "ID" "finish" text should "contain" "Hello World"

  # ── Element Count ─────────────────────────────────────────────────────────────
  Scenario: Dynamic Elements and Count Assertions
    Given I navigate to URL "https://the-internet.herokuapp.com/add_remove_elements/"
    When I click on element with "XPATH" "//button[text()='Add Element']"
    And I click on element with "XPATH" "//button[text()='Add Element']"
    And I click on element with "XPATH" "//button[text()='Add Element']"
    Then Element count with "CSS" "#elements button" should be 3
    And Page source should "contain" "Delete"

  # ── JavaScript Execution ──────────────────────────────────────────────────────
  Scenario: JavaScript Execution and Attribute Manipulation
    Given I navigate to URL "https://the-internet.herokuapp.com/inputs"
    When I execute JavaScript "document.querySelector('input[type=number]').value='99';"
    Then Element with "CSS" "input[type='number']" attribute "value" should "be" "99"
    When I highlight element with "CSS" "input[type='number']"
    And I set attribute "data-gheetah" of element with "CSS" "input[type='number']" to "automation"
    Then Element with "CSS" "input[type='number']" attribute "data-gheetah" should "be" "automation"

  # ── Keyboard Shortcuts ────────────────────────────────────────────────────────
  Scenario: Keyboard Actions and Shortcuts
    Given I navigate to URL "https://the-internet.herokuapp.com/key_presses"
    When I click on element with "ID" "target"
    And I press "Enter" key
    Then Element with "ID" "result" text should "contain" "ENTER"
    When I press "XPATH+//body+a" key combination
    When I press "Tab" key
    Then Element with "ID" "result" text should "contain" "TAB"

  # ── Multi-Window ─────────────────────────────────────────────────────────────
  Scenario: Multi-Window and Tab Management
    Given I navigate to URL "https://the-internet.herokuapp.com/windows"
    When I click on element with "LINKTEXT" "Click Here"
    And I switch to tab 1
    Then Page title should "contain" "New Window"
    When I close current tab
    And I switch to main window
    Then Page title should "contain" "Opening a new window"
