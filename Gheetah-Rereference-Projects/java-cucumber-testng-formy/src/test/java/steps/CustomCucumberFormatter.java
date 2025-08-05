package steps;

import io.cucumber.plugin.ConcurrentEventListener;
import io.cucumber.plugin.event.*;
import java.io.*;
import java.nio.file.*;
import java.text.SimpleDateFormat;
import java.util.*;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

public class CustomCucumberFormatter implements ConcurrentEventListener {
    private static final Logger logger = LoggerFactory.getLogger(CustomCucumberFormatter.class);
    private final List<String> stdOutMessages = new ArrayList<>();
    private String outputFilePath;
    private String scenarioTag;

    public CustomCucumberFormatter() {
        logger.info("Initializing CustomCucumberFormatter");
    }

    @Override
    public void setEventPublisher(EventPublisher publisher) {
        logger.info("Setting event publisher for CustomCucumberFormatter");
        publisher.registerHandlerFor(TestCaseStarted.class, this::handleTestCaseStarted);
        publisher.registerHandlerFor(TestStepStarted.class, this::handleTestStepStarted);
        publisher.registerHandlerFor(TestStepFinished.class, this::handleTestStepFinished);
        publisher.registerHandlerFor(TestCaseFinished.class, this::handleTestCaseFinished);
    }

    private void handleTestCaseStarted(TestCaseStarted event) {
        TestCase testCase = event.getTestCase();
        scenarioTag = testCase.getTags().stream()
            .filter(tag -> tag.startsWith("@"))
            .findFirst()
            .map(tag -> tag.substring(1))
            .orElse("UnknownTag");
        String timestamp = new SimpleDateFormat("yyyyMMdd_HHmmss").format(new Date());
        outputFilePath = String.format("TestResults/%s_%s_test_results.xml", scenarioTag, timestamp);
        stdOutMessages.clear();
        stdOutMessages.add("Scenario: " + testCase.getName());
        logger.info("Test case started: {}, Output file: {}", testCase.getName(), outputFilePath);
    }

    private void handleTestStepStarted(TestStepStarted event) {
        if (event.getTestStep() instanceof PickleStepTestStep step) {
            stdOutMessages.add(step.getStep().getKeyword() + step.getStep().getText());
            logger.info("Test step started: {}", step.getStep().getText());
        }
    }

    private void handleTestStepFinished(TestStepFinished event) {
        if (event.getTestStep() instanceof PickleStepTestStep step) {
            Status status = event.getResult().getStatus();
            String statusMessage = switch (status) {
                case PASSED -> "-> done: " + step.getStep().getText() + " (" + event.getResult().getDuration().toMillis() / 1000.0 + "s)";
                case FAILED -> "-> error: " + event.getResult().getError().getMessage();
                default -> "-> " + status.name().toLowerCase() + ": " + step.getStep().getText();
            };
            stdOutMessages.add(statusMessage);
            logger.info("Test step finished: {}", statusMessage);
        }
    }

    private void handleTestCaseFinished(TestCaseFinished event) {
        logger.info("Test case finished, writing XML report to: {}", outputFilePath);
        try {
            Files.createDirectories(Paths.get("TestResults"));
            logger.info("TestResults directory created or exists");
            try (PrintWriter writer = new PrintWriter(new FileWriter(outputFilePath))) {
                writer.println("<Results xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\">");
                writer.println("    <UnitTestResult>");
                writer.println("        <Output>");
                writer.println("            <StdOut>" + String.join("\n", stdOutMessages) + "</StdOut>");
                writer.println("        </Output>");
                writer.println("    </UnitTestResult>");
                writer.println("</Results>");
                logger.info("XML report written successfully to: {}", outputFilePath);
            }
        } catch (IOException e) {
            logger.error("Failed to write XML report: {}", e.getMessage(), e);
            throw new RuntimeException("Failed to write XML report: " + e.getMessage(), e);
        }
    }
}