package {{ProjectName}}.hooks;

import {{ProjectName}}.drivers.DriverFactory;
import io.cucumber.java.After;
import io.cucumber.java.Scenario;
import org.openqa.selenium.OutputType;
import org.openqa.selenium.TakesScreenshot;

public class Hooks {
    @After("@Mobile")
    public void tearDown(Scenario scenario) {
        if (scenario.isFailed() && DriverFactory.getDriver() != null) {
            byte[] screenshot = ((TakesScreenshot) DriverFactory.getDriver()).getScreenshotAs(OutputType.BYTES);
            scenario.attach(screenshot, "image/png", "Screenshot on Failure");
        }
        DriverFactory.quitDriver();
    }
}