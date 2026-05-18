@Web
Feature: Gheetah Java Web Automation Showcase

  Scenario: Comprehensive Web Search and Verification
    Given I navigate to URL "https://automationexercise.com"
    Then Page title should "contain" "Automation Exercise"
    
    When I click on element with "TEXT" "Products"
    And I enter "Blue Top" into element with "ID" "search_product"
    And I click on element with "ID" "submit_search"
    
    Then Element with "CSS" ".productinfo p" text should "contain" "Blue Top"
    And Element with "XPATH" "//a[text()='View Product']" should be "visible"