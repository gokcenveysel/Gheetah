package runner;

import io.cucumber.testng.AbstractTestNGCucumberTests;
import io.cucumber.testng.CucumberOptions;

@CucumberOptions(
    features = "src/test/resources/features",
    glue = {"steps"},
    plugin = {"pretty", "steps.CustomCucumberFormatter"},
    monochrome = true
)
public class CucumberRunner extends AbstractTestNGCucumberTests {
}