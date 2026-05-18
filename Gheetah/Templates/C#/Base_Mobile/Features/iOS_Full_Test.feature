@Mobile @iOS
Feature: iOS Application and Safari Automation

  Scenario: Safari Browser and System Interactions
    Given I launch iOS application "com.apple.mobilesafari" on device "iPhone 15 Pro"
    
    When I enter "https://gheetah.io" into mobile field AccessibilityId "URL"
    And I tap on mobile element with XPath "//XCUIElementTypeButton[@name='Go']"
    
    Then Mobile element with AccessibilityId "Gheetah_Logo" should be visible
    And Mobile element with Text "Gheetah" should be enabled
    
    When I rotate device to LANDSCAPE
    And I swipe down
    And I swipe left
    
    Then Mobile element with AccessibilityId "Footer_Contact" should be enabled
    
    # Extra iOS interactions
    When I hide keyboard
    And I tap on mobile element with Text "Share"