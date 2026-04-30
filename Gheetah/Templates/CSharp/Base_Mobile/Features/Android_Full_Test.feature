@Mobile @Android
Feature: Android E-Commerce Application Test

  Scenario: User Login and Product Interaction
    # Uygulamayı parametrik olarak başlat (Gheetah Parametrik Executor)
    Given I launch Android application "C:\Gheetah\Apps\ecommerce.apk" on device "Pixel_7_Pro"
    
    # Form doldurma ve Klavye yönetimi
    When I enter "gheetah_tester" into mobile field AccessibilityId "username_input"
    And I enter "SecurePass123!" into mobile field ID "com.ecommerce:id/password_input"
    And I hide keyboard
    And I tap on mobile element with Text "LOGIN"
    
    # Sayfa doğrulaması
    Then Mobile element AccessibilityId "home_screen_header" should be visible
    
    # Jestler (Swipe ve Kaydırma)
    When I swipe up
    And I swipe right
    And I tap on mobile element with XPath "//android.widget.TextView[@text='Featured Product']"
    
    # Ekran oryantasyonu ve son doğrulama
    And I rotate device to LANDSCAPE
    Then Mobile element ID "com.ecommerce:id/product_description" should contain text "High Quality"