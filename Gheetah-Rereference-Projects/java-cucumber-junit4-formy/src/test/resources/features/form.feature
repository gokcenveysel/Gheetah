Feature: Form Submission

  Scenario: Submit the Formy form with valid data
    Given I navigate to the Formy form page
    When I fill the form with first name "John", last name "Doe", job title "Engineer", and date "07/22/2025"
    When I submit the form
    Then I should see the thank you page

  Scenario: Complete the autocomplete form with valid address
    Given I am on the Formy autocomplete page
    When I enter a valid address in the autocomplete field
    And I select the first suggested address
    Then The address fields should be auto-populated