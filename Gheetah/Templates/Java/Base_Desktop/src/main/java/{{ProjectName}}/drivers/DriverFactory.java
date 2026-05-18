package {{ProjectName}}.drivers;

import io.appium.java_client.windows.WindowsDriver;
import org.openqa.selenium.remote.DesiredCapabilities;
import java.net.URL;
import java.time.Duration;

public class DriverFactory {
    private static WindowsDriver driver;

    public static WindowsDriver createDriver(String appPathOrId) throws Exception {
        DesiredCapabilities caps = new DesiredCapabilities();
        // appPathOrId: .exe yolu veya Windows AppId (örn: Microsoft.WindowsCalculator_8wekyb3d8bbwe!App)
        caps.setCapability("app", appPathOrId);
        caps.setCapability("platformName", "Windows");
        caps.setCapability("deviceName", "WindowsPC");
        caps.setCapability("ms:waitForAppLaunch", "5");

        // WinAppDriver varsayılan olarak 4723 portunda çalışır
        URL url = new URL("http://127.0.0.1:4723/");
        
        driver = new WindowsDriver(url, caps);
        driver.manage().timeouts().implicitlyWait(Duration.ofSeconds(10));
        return driver;
    }

    public static WindowsDriver getDriver() { return driver; }

    public static void quitDriver() {
        if (driver != null) {
            driver.quit();
            driver = null;
        }
    }
}