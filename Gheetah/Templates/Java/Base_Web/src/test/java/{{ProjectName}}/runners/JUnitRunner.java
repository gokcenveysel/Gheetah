package {{ProjectName}}.runners;

import org.junit.platform.suite.api.ConfigurationParameter;
import org.junit.platform.suite.api.IncludeEngines;
import org.junit.platform.suite.api.SelectClasspathResource;
import org.junit.platform.suite.api.Suite;
import static io.cucumber.junit.platform.engine.Constants.GLUE_PROPERTY_NAME;
import static io.cucumber.junit.platform.engine.Constants.PLUGIN_PROPERTY_NAME;

/**
 * Gheetah JUnit 5 Test Runner.
 * Bu sınıf, JUnit Platform Suite Engine kullanarak Cucumber testlerini tetikler.
 * Tüm konfigürasyon anotasyonlar üzerinden yönetildiği için sınıf gövdesi boştur.
 */
@Suite
@IncludeEngines("cucumber")
@SelectClasspathResource("features")
@ConfigurationParameter(key = GLUE_PROPERTY_NAME, value = "{{ProjectName}}.stepdefinitions,{{ProjectName}}.hooks")
@ConfigurationParameter(key = PLUGIN_PROPERTY_NAME, value = "pretty, html:target/cucumber-reports/junit/report.html, json:target/cucumber-reports/junit/report.json")
public class JUnitRunner {
}