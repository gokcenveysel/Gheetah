package {{ProjectName}}.hooks;

import {{ProjectName}}.drivers.DriverFactory;
import io.cucumber.java.After;

public class Hooks {
    @After("@Desktop")
    public void tearDown() {
        // Desktop oturumunu kapatır
        DriverFactory.quitDriver();
    }
}