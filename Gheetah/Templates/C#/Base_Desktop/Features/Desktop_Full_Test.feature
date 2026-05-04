@Desktop
Feature: Windows Desktop Application Orchestration

  Scenario: Calculator Operations
    Given I launch desktop application at "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App"
    When I maximize the desktop window
    And I click desktop element with Name "Five"
    And I click desktop element with Name "Plus"
    And I click desktop element with Name "Seven"
    And I click desktop element with Name "Equals"
    
    Then Desktop element with Name "Display is 12" should exist
    And Desktop element with Name "Display is 12" should be visible

  Scenario: Notepad Text Operations and Window Management
    Given I launch desktop application at "notepad.exe"
    When I maximize the desktop window
    And I type "Gheetah Desktop Automation Test Report" into desktop element Name "Text Editor"
    And I press Enter key
    And I type "This is a comprehensive test of Gheetah framework capabilities." into desktop element Name "Text Editor"
    
    When I right click desktop element with Name "Text Editor"
    And I press Escape key
    And I switch to window with Title "Notepad"
    
    Then Desktop element with Name "Text Editor" should exist
    And Desktop element with Name "Text Editor" text should contain "Gheetah"

  Scenario: Multi-Window Desktop Workflow
    Given I launch desktop application at "notepad.exe"
    When I type "First Window Test" into desktop element Name "Text Editor"
    And I press Enter key
    And I minimize the desktop window
    And I restore the desktop window
    Then Desktop element with Name "Text Editor" should be visible