package {{ProjectName}}.runners;

import io.cucumber.testng.AbstractTestNGCucumberTests;
import io.cucumber.testng.CucumberOptions;
import org.testng.annotations.DataProvider;

@CucumberOptions(
    features = "src/test/resources/features",
    glue = {"{{ProjectName}}.stepdefinitions", "{{ProjectName}}.hooks"},
    plugin = {
        "pretty",
        "html:target/cucumber-reports/testng/report.html",
        "json:target/cucumber-reports/testng/report.json"
    },
    monochrome = true
)
public class TestNGRunner extends AbstractTestNGCucumberTests {
    
    @Override
    @DataProvider(parallel = true)
    public Object[][] scenarios() {
        return super.scenarios();
    }
}