package {{ProjectName}}.hooks;

import {{ProjectName}}.drivers.DriverFactory;
import io.cucumber.java.After;
import io.cucumber.java.Before;
import io.cucumber.java.Scenario;

public class Hooks {
    @Before("@Web")
    public void setup() {
        // Gheetah varsayılan olarak Chrome başlatır
        DriverFactory.createDriver("chrome");
    }

    @After("@Web")
    public void tearDown(Scenario scenario) {
        // Hata durumunda ekran görüntüsü alma buraya eklenebilir
        DriverFactory.quitDriver();
    }
}