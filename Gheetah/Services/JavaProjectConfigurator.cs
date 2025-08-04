using Gheetah.Services;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

public class JavaProjectConfigurator
{
    private readonly ILogService _logService;

    public JavaProjectConfigurator(ILogService logService)
    {
        _logService = logService;
    }

    public async Task ConfigureJavaProject(string projectPath)
    {
        await _logService.LogAsync("SYSTEM", "ConfigureJavaProject", $"Configuring Java project at {projectPath}");

        var pomFiles = Directory.GetFiles(projectPath, "pom.xml", SearchOption.AllDirectories);
        if (!pomFiles.Any())
        {
            await _logService.LogAsync("SYSTEM", "ConfigureJavaProject", $"No pom.xml found in {projectPath}");
            return;
        }

        string pomFile = pomFiles.First();
        string projectType = await DetectProjectTypeAsync(projectPath, pomFile);
        await _logService.LogAsync("SYSTEM", "ConfigureJavaProject", $"Detected project type: {projectType}");

        await EnsureCucumberDependenciesAsync(pomFile, projectType);
        await UpdateJavaFilesAsync(projectPath, pomFile, projectType);
    }

    private async Task<string> DetectProjectTypeAsync(string projectPath, string pomFile)
    {
        if (File.Exists(pomFile))
        {
            string pomContent = await File.ReadAllTextAsync(pomFile);
            if (pomContent.Contains("io.cucumber:cucumber-junit") || pomContent.Contains("junit:junit"))
            {
                await _logService.LogAsync("SYSTEM", "DetectProjectTypeAsync", $"Detected JUnit4 based on pom.xml: {pomFile}");
                return "JUnit4";
            }
            if (pomContent.Contains("io.cucumber:cucumber-testng") || pomContent.Contains("org.testng:testng"))
            {
                await _logService.LogAsync("SYSTEM", "DetectProjectTypeAsync", $"Detected TestNG based on pom.xml: {pomFile}");
                return "TestNG";
            }
        }

        if (File.Exists(Path.Combine(projectPath, "testng.xml")))
        {
            await _logService.LogAsync("SYSTEM", "DetectProjectTypeAsync", $"Detected TestNG based on testng.xml in {projectPath}");
            return "TestNG";
        }

        string srcTestJavaPath = Path.Combine(projectPath, "src", "test", "java");
        if (Directory.Exists(srcTestJavaPath))
        {
            var javaFiles = Directory.GetFiles(srcTestJavaPath, "*.java", SearchOption.AllDirectories);
            foreach (var javaFile in javaFiles)
            {
                string content = await File.ReadAllTextAsync(javaFile);
                if (content.Contains("org.junit.runner.RunWith") || content.Contains("io.cucumber.junit.Cucumber"))
                {
                    await _logService.LogAsync("SYSTEM", "DetectProjectTypeAsync", $"Detected JUnit4 based on runner file: {javaFile}");
                    return "JUnit4";
                }
                if (content.Contains("AbstractTestNGCucumberTests") || content.Contains("io.cucumber.testng"))
                {
                    await _logService.LogAsync("SYSTEM", "DetectProjectTypeAsync", $"Detected TestNG based on runner file: {javaFile}");
                    return "TestNG";
                }
            }
        }

        await _logService.LogAsync("SYSTEM", "DetectProjectTypeAsync", $"Unable to determine project type for {projectPath}, defaulting to TestNG");
        return "TestNG";
    }

    private async Task EnsureCucumberDependenciesAsync(string pomFile, string projectType)
    {
        try
        {
            var doc = XDocument.Load(pomFile);
            var ns = doc.Root.Name.Namespace;

            var dependencies = doc.Descendants(ns + "dependencies").FirstOrDefault();
            if (dependencies == null)
            {
                dependencies = new XElement(ns + "dependencies");
                doc.Root.Add(dependencies);
                await _logService.LogAsync("SYSTEM", "EnsureCucumberDependenciesAsync", $"Created new <dependencies> section in {pomFile}");
            }

            var oldDependencies = dependencies.Descendants(ns + "dependency")
                .Where(dep =>
                    dep.Element(ns + "groupId")?.Value == "info.cukes" ||
                    dep.Element(ns + "groupId")?.Value == "io.cucumber" ||
                    dep.Element(ns + "groupId")?.Value == "junit" ||
                    dep.Element(ns + "groupId")?.Value == "org.junit.jupiter" ||
                    dep.Element(ns + "groupId")?.Value == "org.testng")
                .ToList();
            foreach (var dep in oldDependencies)
            {
                await _logService.LogAsync("SYSTEM", "EnsureCucumberDependenciesAsync", $"Removed old dependency: {dep}");
                dep.Remove();
            }

            var commonDependencies = new[]
            {
                new { GroupId = "io.cucumber", ArtifactId = "cucumber-java", Version = "7.20.1" },
                new { GroupId = "io.cucumber", ArtifactId = "cucumber-picocontainer", Version = "7.20.1" },
                new { GroupId = "org.hamcrest", ArtifactId = "hamcrest", Version = "2.2" },
                new { GroupId = "org.slf4j", ArtifactId = "slf4j-simple", Version = "2.0.16" },
                new { GroupId = "org.seleniumhq.selenium", ArtifactId = "selenium-java", Version = "4.27.0" }
            };

            var frameworkDependencies = projectType == "JUnit4" ? new[]
            {
                new { GroupId = "io.cucumber", ArtifactId = "cucumber-junit", Version = "7.20.1" },
                new { GroupId = "junit", ArtifactId = "junit", Version = "4.13.2" }
            } : new[]
            {
                new { GroupId = "io.cucumber", ArtifactId = "cucumber-testng", Version = "7.20.1" },
                new { GroupId = "org.testng", ArtifactId = "testng", Version = "7.10.2" }
            };

            foreach (var reqDep in commonDependencies.Concat(frameworkDependencies))
            {
                var existingDep = dependencies.Descendants(ns + "dependency")
                    .FirstOrDefault(dep =>
                        dep.Element(ns + "groupId")?.Value == reqDep.GroupId &&
                        dep.Element(ns + "artifactId")?.Value == reqDep.ArtifactId);

                if (existingDep == null)
                {
                    var newDep = new XElement(ns + "dependency",
                        new XElement(ns + "groupId", reqDep.GroupId),
                        new XElement(ns + "artifactId", reqDep.ArtifactId),
                        new XElement(ns + "version", reqDep.Version));
                    dependencies.Add(newDep);
                    await _logService.LogAsync("SYSTEM", "EnsureCucumberDependenciesAsync", $"Added {reqDep.GroupId}:{reqDep.ArtifactId}:{reqDep.Version} to {pomFile}");
                }
                else if (existingDep.Element(ns + "version")?.Value != reqDep.Version)
                {
                    existingDep.Element(ns + "version").Value = reqDep.Version;
                    await _logService.LogAsync("SYSTEM", "EnsureCucumberDependenciesAsync", $"Updated {reqDep.GroupId}:{reqDep.ArtifactId} to version {reqDep.Version} in {pomFile}");
                }
            }

            var build = doc.Descendants(ns + "build").FirstOrDefault();
            if (build == null)
            {
                build = new XElement(ns + "build");
                doc.Root.Add(build);
                await _logService.LogAsync("SYSTEM", "EnsureCucumberDependenciesAsync", $"Created new <build> section in {pomFile}");
            }

            var plugins = build.Descendants(ns + "plugins").FirstOrDefault();
            if (plugins == null)
            {
                plugins = new XElement(ns + "plugins");
                build.Add(plugins);
                await _logService.LogAsync("SYSTEM", "EnsureCucumberDependenciesAsync", $"Created new <plugins> section in {pomFile}");
            }

            plugins.Descendants(ns + "plugin")
                .Where(p => p.Element(ns + "artifactId")?.Value == "maven-surefire-plugin")
                .ToList()
                .ForEach(p => p.Remove());

            var surefirePlugin = new XElement(ns + "plugin",
                new XElement(ns + "groupId", "org.apache.maven.plugins"),
                new XElement(ns + "artifactId", "maven-surefire-plugin"),
                new XElement(ns + "version", "3.2.5"),
                new XElement(ns + "configuration",
                    new XElement(ns + "systemPropertyVariables",
                        new XElement(ns + "cucumber.filter.tags", "${cucumber.filter.tags}")
                    )
                )
            );

            if (projectType == "TestNG")
            {
                surefirePlugin.Element(ns + "configuration").Add(
                    new XElement(ns + "suiteXmlFiles",
                        new XElement(ns + "suiteXmlFile", "testng.xml")
                    )
                );
            }

            plugins.Add(surefirePlugin);
            await _logService.LogAsync("SYSTEM", "EnsureCucumberDependenciesAsync", $"Added maven-surefire-plugin for {projectType} to {pomFile}");

            if (!plugins.Descendants(ns + "plugin").Any(p => p.Element(ns + "artifactId")?.Value == "maven-compiler-plugin"))
            {
                var compilerPlugin = new XElement(ns + "plugin",
                    new XElement(ns + "groupId", "org.apache.maven.plugins"),
                    new XElement(ns + "artifactId", "maven-compiler-plugin"),
                    new XElement(ns + "version", "3.13.0"),
                    new XElement(ns + "configuration",
                        new XElement(ns + "source", "17"),
                        new XElement(ns + "target", "17")
                    )
                );
                plugins.Add(compilerPlugin);
                await _logService.LogAsync("SYSTEM", "EnsureCucumberDependenciesAsync", $"Added maven-compiler-plugin to {pomFile}");
            }

            doc.Save(pomFile);
            await _logService.LogAsync("SYSTEM", "EnsureCucumberDependenciesAsync", $"Updated {pomFile} for {projectType}");
        }
        catch (Exception ex)
        {
            await _logService.LogAsync("SYSTEM", "EnsureCucumberDependenciesAsync", $"Error updating {pomFile}: {ex.Message}");
        }
    }

    private async Task UpdateJavaFilesAsync(string projectPath, string pomFile, string projectType)
    {
        var projectRoot = Path.GetDirectoryName(pomFile);
        var srcTestJavaPath = Path.Combine(projectRoot, "src", "test", "java");
        var javaFiles = Directory.Exists(srcTestJavaPath)
            ? Directory.GetFiles(srcTestJavaPath, "*.java", SearchOption.AllDirectories)
            : Array.Empty<string>();
        
        var runnerClasses = new List<string>();
        var processedFiles = new List<string>();

        if (!Directory.Exists(srcTestJavaPath))
        {
            await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"src/test/java directory not found in {projectPath}, skipping Java file updates");
            return;
        }

        foreach (var javaFile in javaFiles)
        {
            try
            {
                var content = await File.ReadAllTextAsync(javaFile);
                var preview = content.Length > 1000 ? content.Substring(0, 1000) : content;
                await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"Processing {javaFile}, content preview:\n{preview}");

                var lines = content.Split('\n').Select(l => l.TrimEnd()).ToList();
                var newLines = new List<string>(lines);
                bool modified = false;
                bool hasCucumberOptions = false;
                bool hasAssertThat = false;
                string packageName = null;
                string className = null;

                var contentWithoutComments = Regex.Replace(content, @"//.*?$|/\*.*?\*/", "", RegexOptions.Multiline | RegexOptions.Singleline);
                var cucumberOptionsMatch = Regex.Match(contentWithoutComments, @"@CucumberOptions\s*\((.*?)\)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (cucumberOptionsMatch.Success)
                {
                    hasCucumberOptions = true;
                    var optionsContent = cucumberOptionsMatch.Groups[1].Value;
                    await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"Found @CucumberOptions in {javaFile}: {optionsContent}");
                }
                else
                {
                    await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"No @CucumberOptions found in {javaFile}");
                }

                for (int i = 0; i < lines.Count; i++)
                {
                    var trimmedLine = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("//")) continue;

                    if (packageName == null && trimmedLine.StartsWith("package ", StringComparison.OrdinalIgnoreCase))
                    {
                        var packageMatch = Regex.Match(trimmedLine, @"package\s+([\w\.]+)\s*;", RegexOptions.IgnoreCase);
                        if (packageMatch.Success)
                        {
                            packageName = packageMatch.Groups[1].Value;
                            if (!Regex.IsMatch(packageName, @"^[\w\.]+$"))
                            {
                                await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"Invalid package name in {javaFile}: {packageName}");
                                packageName = null;
                            }
                        }
                    }

                    if (className == null && trimmedLine.StartsWith("public class ", StringComparison.OrdinalIgnoreCase))
                    {
                        var classMatch = Regex.Match(trimmedLine, @"public\s+class\s+(\w+)\s*(extends\s+[\w\s]+)?\{?", RegexOptions.IgnoreCase);
                        if (classMatch.Success)
                        {
                            className = classMatch.Groups[1].Value;
                            if (classMatch.Groups[2].Success && !trimmedLine.Contains(projectType == "TestNG" ? "extends AbstractTestNGCucumberTests" : "extends", StringComparison.OrdinalIgnoreCase))
                            {
                                await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"Class {className} in {javaFile} already extends another class: {classMatch.Groups[2].Value}");
                            }
                        }
                    }

                    if (trimmedLine.StartsWith("import ", StringComparison.OrdinalIgnoreCase))
                    {
                        if (Regex.IsMatch(trimmedLine, @"cucumber\.api\.", RegexOptions.IgnoreCase))
                        {
                            newLines[i] = lines[i].Replace("cucumber.api.", "io.cucumber.", StringComparison.OrdinalIgnoreCase);
                            modified = true;
                        }
                        else if (projectType == "TestNG" && Regex.IsMatch(trimmedLine, @"org\.junit\.runner\.RunWith|io\.cucumber\.junit\.Cucumber", RegexOptions.IgnoreCase))
                        {
                            newLines[i] = "import io.cucumber.testng.CucumberOptions; import io.cucumber.testng.AbstractTestNGCucumberTests;";
                            modified = true;
                        }
                        else if (projectType == "JUnit4" && Regex.IsMatch(trimmedLine, @"io\.cucumber\.testng\.AbstractTestNGCucumberTests|io\.cucumber\.testng\.CucumberOptions", RegexOptions.IgnoreCase))
                        {
                            newLines[i] = "import org.junit.runner.RunWith; import io.cucumber.junit.Cucumber; import io.cucumber.junit.CucumberOptions;";
                            modified = true;
                        }
                    }

                    if (projectType == "TestNG" && Regex.IsMatch(trimmedLine, @"@RunWith\s*\(\s*Cucumber\.class\s*\)", RegexOptions.IgnoreCase))
                    {
                        newLines[i] = "";
                        modified = true;
                    }
                    else if (projectType == "JUnit4" && !trimmedLine.Contains("@RunWith(Cucumber.class)") && hasCucumberOptions)
                    {
                        int insertIndex = i;
                        newLines.Insert(insertIndex, "@RunWith(Cucumber.class)");
                        modified = true;
                    }

                    if (hasCucumberOptions && Regex.IsMatch(trimmedLine, @"@CucumberOptions\s*\(", RegexOptions.IgnoreCase))
                    {
                        var optionsContent = cucumberOptionsMatch.Groups[1].Value;
                        var newOptions = optionsContent;

                        if (!Regex.IsMatch(optionsContent, @"tags\s*=", RegexOptions.IgnoreCase))
                        {
                            newOptions = $"tags = \"\", {optionsContent}";
                        }
                        if (!Regex.IsMatch(optionsContent, @"features\s*=", RegexOptions.IgnoreCase))
                        {
                            newOptions = $"features = \"src/test/resources/features\", {newOptions}";
                        }
                        if (!Regex.IsMatch(optionsContent, @"glue\s*=", RegexOptions.IgnoreCase))
                        {
                            newOptions = $"glue = \"{packageName ?? "steps"}\", {newOptions}";
                        }

                        if (newOptions != optionsContent)
                        {
                            newLines[i] = trimmedLine.Replace(optionsContent, newOptions);
                            modified = true;
                            await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"Updated @CucumberOptions in {javaFile}: {newOptions}");
                        }
                    }

                    if (trimmedLine.Contains("assertThat", StringComparison.OrdinalIgnoreCase))
                    {
                        hasAssertThat = true;
                    }
                }

                if (projectType == "TestNG" && hasCucumberOptions && className != null && !lines.Any(l => Regex.IsMatch(l, $@"public\s+class\s+{Regex.Escape(className)}\s+extends", RegexOptions.IgnoreCase)))
                {
                    for (int i = 0; i < newLines.Count; i++)
                    {
                        if (Regex.IsMatch(newLines[i], $@"public\s+class\s+{Regex.Escape(className)}\s*(?!\w*extends)", RegexOptions.IgnoreCase))
                        {
                            newLines[i] = newLines[i].Replace($"public class {className}", $"public class {className} extends AbstractTestNGCucumberTests", StringComparison.OrdinalIgnoreCase);
                            modified = true;
                            break;
                        }
                    }
                }

                if (hasAssertThat && !lines.Any(l => l.Contains("org.hamcrest.MatcherAssert.assertThat", StringComparison.OrdinalIgnoreCase)))
                {
                    int insertIndex = packageName != null ? 1 : 0;
                    newLines.Insert(insertIndex, "import static org.hamcrest.MatcherAssert.assertThat;");
                    newLines.Insert(insertIndex + 1, "import static org.hamcrest.Matchers.*;");
                    modified = true;
                }

                if (hasCucumberOptions && className != null)
                {
                    var relativePath = javaFile.Replace(srcTestJavaPath, "").TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    var expectedPackage = Path.GetDirectoryName(relativePath)?.Replace(Path.DirectorySeparatorChar, '.').Replace(Path.AltDirectorySeparatorChar, '.');
                    var expectedClass = Path.GetFileNameWithoutExtension(relativePath);
                    var runnerClass = expectedPackage != null ? $"{expectedPackage}.{expectedClass}" : expectedClass;

                    if (!string.Equals(className, expectedClass, StringComparison.OrdinalIgnoreCase))
                    {
                        await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"Class name mismatch in {javaFile}: expected {expectedClass}, found {className}");
                    }
                    else if (packageName != null && !string.Equals(packageName, expectedPackage, StringComparison.OrdinalIgnoreCase))
                    {
                        await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"Package mismatch in {javaFile}: expected {expectedPackage}, found {packageName}");
                    }
                    else if (!cucumberOptionsMatch.Success || !Regex.IsMatch(cucumberOptionsMatch.Groups[1].Value, @"features\s*=", RegexOptions.IgnoreCase) || !Regex.IsMatch(cucumberOptionsMatch.Groups[1].Value, @"glue\s*=", RegexOptions.IgnoreCase))
                    {
                        await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"Invalid @CucumberOptions in {javaFile}: missing features or glue");
                    }
                    else if (Regex.IsMatch(runnerClass, @"^[\w\.]+\.\w+$|^[\w]+$"))
                    {
                        runnerClasses.Add(runnerClass);
                        await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"Found valid runner class: {runnerClass} in {javaFile}");
                    }
                    else
                    {
                        await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"Invalid runner class in {javaFile}: {runnerClass}");
                    }
                }

                if (modified)
                {
                    await File.WriteAllTextAsync(javaFile, string.Join("\n", newLines));
                    await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"Updated {javaFile} for {projectType}");
                    processedFiles.Add(javaFile);
                }
            }
            catch (Exception ex)
            {
                await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"Error processing {javaFile}: {ex.Message}");
            }
        }

        var testNgFile = Path.Combine(projectRoot, "testng.xml");
        if (projectType == "TestNG" && runnerClasses.Any())
        {
            try
            {
                var testNgContent = new StringBuilder();
                testNgContent.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                testNgContent.AppendLine("<!DOCTYPE suite SYSTEM \"https://testng.org/testng-1.0.dtd\">");
                testNgContent.AppendLine("<suite name=\"Cucumber Suite\">");
                testNgContent.AppendLine("    <test name=\"Cucumber Test\">");
                testNgContent.AppendLine("        <classes>");
                foreach (var runnerClass in runnerClasses)
                {
                    testNgContent.AppendLine($"            <class name=\"{runnerClass}\" />");
                }
                testNgContent.AppendLine("        </classes>");
                testNgContent.AppendLine("    </test>");
                testNgContent.AppendLine("</suite>");

                await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"Preparing to write testng.xml with classes: {string.Join(", ", runnerClasses)}");
                await File.WriteAllTextAsync(testNgFile, testNgContent.ToString());
                await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"Created {testNgFile} with classes: {string.Join(", ", runnerClasses)}");

                if (!File.Exists(testNgFile))
                {
                    await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"Failed to create {testNgFile}: File does not exist after write attempt");
                }
                else
                {
                    var content = await File.ReadAllTextAsync(testNgFile);
                    await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"testng.xml content:\n{content}");
                }
            }
            catch (Exception ex)
            {
                await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"Error creating {testNgFile}: {ex.Message}");
            }
        }
        else if (projectType == "JUnit4" && File.Exists(testNgFile))
        {
            try
            {
                File.Delete(testNgFile);
                await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"Deleted existing {testNgFile} for JUnit4 project");
            }
            catch (Exception ex)
            {
                await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"Error deleting {testNgFile}: {ex.Message}");
            }
        }

        if (!runnerClasses.Any())
        {
            await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"No valid Cucumber runner classes found in {javaFiles.Length} Java files");
            await _logService.LogAsync("SYSTEM", "UpdateJavaFilesAsync", $"Processed files: {(processedFiles.Any() ? string.Join(", ", processedFiles) : "None")}");
        }
    }
}