@Mobile @iOS
Feature: iOS Safari and System App Test

  Scenario: Safari Browser Automation
    # BundleId kullanarak sistem uygulamasını başlat
    Given I launch iOS application "com.apple.mobilesafari" on device "iPhone 15 Pro"
    
    # iOS özel seçicilerle URL girişi
    When I enter "https://gheetah.io" into mobile field AccessibilityId "URL"
    And I tap on mobile element with XPath "//XCUIElementTypeButton[@name='Go']"
    
    # Görünürlük kontrolü
    Then Mobile element AccessibilityId "Gheetah_Logo" should be visible
    
    # Sistem etkileşimi
    When I rotate device to LANDSCAPE
    And I swipe down
    Then Mobile element AccessibilityId "Footer_Contact" should be enabled