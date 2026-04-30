@Mobile
Feature: Gheetah Mobile Automation Demo

  Scenario: Comprehensive Mobile Element Interactions
    Given I launch "Android" application "com.example.sampleapp" on device "Pixel_7_Pro"
    # Not: iOS için "iOS" ve "com.bundle.id" şeklinde güncellenebilir.
    
    When I tap on mobile element with "ACCESSIBILITYID" "login_button"
    And I enter "gheetah_user" into mobile element with "ID" "username_field"
    And I enter "password123" into mobile element with "XPATH" "//android.widget.EditText[@content-desc='pass']"
    And I hide keyboard
    And I tap on mobile element with "TEXT" "SUBMIT"
    
    Then Mobile element with "ID" "dashboard_header" should be visible
    And Mobile element with "ACCESSIBILITYID" "welcome_text" text should be "Welcome, Gheetah User"
    
  Scenario: List and Search Validation
    When I tap on mobile element with "ID" "search_icon"
    And I enter "Laptop" into mobile element with "ID" "search_input"
    Then Mobile element with "TEXT" "MacBook Pro" should be visible