Feature: Web Automation Tests
  As a tester
  I want to verify the functionality of various web pages
  So that I can ensure the website works as expected

Scenario: Add and remove elements on the page
  Given I am on the Add/Remove Elements page
  When I add 3 elements
  And I remove 2 elements
  Then 1 element should remain

Scenario: Drag and drop elements
  Given I am on the Drag and Drop page
  When I drag element A to element B
  Then element B should contain element A

Scenario: Verify floating menu after scrolling
  Given I am on the Floating Menu page
  When I scroll to the bottom of the page
  Then the floating menu should still be visible