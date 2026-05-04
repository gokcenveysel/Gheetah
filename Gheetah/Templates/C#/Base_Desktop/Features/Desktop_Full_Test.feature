@Desktop
Feature: Windows Desktop Application Orchestration

  Scenario: Calculator and Notepad Integration
    # Hesap makinesini AppId/Path ile başlat
    Given I launch desktop application at "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App"
    When I maximize the desktop window
    And I click desktop element with Name "Five"
    And I click desktop element with Name "Plus"
    And I click desktop element with Name "Seven"
    And I click desktop element with Name "Equals"
    
    # Sonuç doğrulaması
    Then Desktop element Name "Display is 12" should exist

  Scenario: System Logs Check via Notepad
    # İkinci bir uygulamayı aynı projede başlat
    Given I launch desktop application at "notepad.exe"
    When I type "Gheetah Desktop Test Report" into desktop element Name "Text Editor"
    
    # Kısayol tuşlarını kullan (Ctrl+S simülasyonu gibi Escape kullanımı)
    And I press Escape key
    And I switch to window with Title "Notepad"
    Then Desktop element Name "Text Editor" should exist