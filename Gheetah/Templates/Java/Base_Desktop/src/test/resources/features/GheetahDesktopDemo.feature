@Desktop
Feature: Gheetah Desktop Automation Demo

  Scenario: Windows Calculator and Notepad Integration
    # Örnek: Windows Hesap Makinesi AppId ile başlatma
    Given I launch desktop application at "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App"
    
    When I maximize the desktop window
    And I click on desktop element with "NAME" "Seven"
    And I click on desktop element with "NAME" "Plus"
    And I click on desktop element with "NAME" "Five"
    And I click on desktop element with "NAME" "Equals"
    
    Then Desktop element with "ID" "CalculatorResults" text should be "Display is 12"

  Scenario: Notepad Text Entry Demo
    Given I launch desktop application at "C:\Windows\System32\notepad.exe"
    
    When I type "Gheetah Orchestration Engine is running!" into desktop element with "CLASS" "Edit"
    And I double click on desktop element with "NAME" "Text Editor"
    
    Then Desktop element with "CLASS" "Edit" should exist