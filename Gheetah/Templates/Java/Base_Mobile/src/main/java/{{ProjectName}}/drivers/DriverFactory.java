package {{ProjectName}}.drivers;

import io.appium.java_client.AppiumDriver;
import io.appium.java_client.android.AndroidDriver;
import io.appium.java_client.ios.IOSDriver;
import org.openqa.selenium.remote.DesiredCapabilities;
import java.net.URL;
import java.time.Duration;

public class DriverFactory {
    private static AppiumDriver driver;

    public static AppiumDriver createDriver(String platform, String appIdentifier, String device) throws Exception {
        DesiredCapabilities caps = new DesiredCapabilities();
        caps.setCapability("platformName", platform);
        caps.setCapability("appium:deviceName", device);
        // iOS için XCUITest, Android için UiAutomator2 (Gheetah standartları)
        caps.setCapability("appium:automationName", platform.equalsIgnoreCase("Android") ? "UiAutomator2" : "XCUITest");
        
        if (appIdentifier.contains("/") || appIdentifier.contains("\\")) {
            caps.setCapability("appium:app", appIdentifier);
        } else {
            caps.setCapability(platform.equalsIgnoreCase("Android") ? "appium:appPackage" : "appium:bundleId", appIdentifier);
        }

        URL remoteUrl = new URL("http://127.0.0.1:4723/");

        // Polimorfizm: Hem Android hem iOS, AppiumDriver'dan türediği için ortak yönetilir
        driver = platform.equalsIgnoreCase("Android") 
                ? new AndroidDriver(remoteUrl, caps) 
                : new IOSDriver(remoteUrl, caps);

        driver.manage().timeouts().implicitlyWait(Duration.ofSeconds(10));
        return driver;
    }

    public static AppiumDriver getDriver() { return driver; }

    public static void quitDriver() {
        if (driver != null) {
            driver.quit();
            driver = null;
        }
    }
}