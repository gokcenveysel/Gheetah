@Mobile @Android
Feature: Android E-Commerce Application Full Test

  Scenario: Complete User Journey with Advanced Interactions
    Given I launch Android application "com.example.ecommerce" on device "Pixel_7_Pro"
    
    # Login
    When I enter "gheetah_tester" into mobile field AccessibilityId "username_input"
    And I enter "SecurePass123!" into mobile field ID "com.ecommerce:id/password_input"
    And I hide keyboard
    And I tap on mobile element with Text "LOGIN"
    
    Then Mobile element with AccessibilityId "home_screen_header" should be visible
    And Mobile element with ID "com.ecommerce:id/welcome_text" text should contain "Welcome"

    # Navigation & Gestures
    When I swipe up
    And I swipe right
    And I tap on mobile element with XPath "//android.widget.TextView[@text='Featured Product']"
    
    # Product Interaction
    And I long press on mobile element with AccessibilityId "product_image" for 2 seconds
    And I tap on mobile element with Text "Add to Cart"
    
    # Cart & Orientation
    When I rotate device to LANDSCAPE
    And I swipe down
    Then Mobile element with ID "com.ecommerce:id/cart_total" should be visible
    And Mobile element with ID "com.ecommerce:id/product_description" text should contain "High Quality"
    
    # Final Actions
    When I tap on mobile element with Text "Checkout"
    And I press Enter key