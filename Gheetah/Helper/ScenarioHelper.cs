using Gheetah.Models.ScenarioModel;
using Gherkin;
using Gherkin.Ast;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Gheetah.Helper
{
    public static class ScenarioHelper
    {
        public static string GenerateHtmlReport(List<TestStep> steps)
        {
            if (steps == null || !steps.Any()) return string.Empty;

            var html = new StringBuilder();

            html.AppendLine(@"
            <style>
                .bdd-report { font-family: system-ui, sans-serif; }
                .status-badges { display:flex; gap:8px; margin-bottom:12px; }
                .badge { padding:3px 8px; border-radius:4px; font-size:12px; font-weight:500; }
                .passed { background:#e6f7ea; color:#2e7d32; }
                .failed { background:#ffebee; color:#c62828; }
                .skipped { background:#fff8e1; color:#e65100; }
                .steps-container { display:flex; flex-direction:column; gap:6px; }
                .step { border-left:2px solid #eee; padding-left:10px; }
                .step.passed { border-color:#4caf50; }
                .step.failed { border-color:#f44336; }
                .step.skipped { border-color:#ff9800; }
                .step-header { 
                    display:flex; align-items:center; gap:10px; padding:6px 0;
                    cursor:pointer; user-select:none;
                }
                .step-icon { flex-shrink:0; }
                .step-text { flex-grow:1; }
                .step-definition { font-weight:500; color:#333; }
                .step-name { color:#666; margin-left:4px; }
                .step-error { color:#d32f2f; font-size:13px; margin-top:2px; }
                .step-duration { color:#999; font-size:11px; }
                .step-toggle { color:#999; font-size:12px; margin-left:20px; margin-right:10px; }
                .step-details { 
                    display: none; 
                    margin-left:24px; 
                    margin-bottom:6px;
                    background:#f8f9fa; 
                    border-radius:4px; 
                    padding:8px;
                    font-size:13px; 
                    font-family: monospace; 
                    white-space: pre-wrap;
                }
                .step.open .step-details {
                    display: block;
                }
            </style>");

            html.AppendLine("<div class='bdd-report'>");

            html.AppendLine($@"
            <div class='status-badges'>
                <span class='badge passed'>{steps.Count(s => s.Status == "Passed")} ✓</span>
                <span class='badge failed'>{steps.Count(s => s.Status == "Failed")} ✕</span>
                <span class='badge skipped'>{steps.Count(s => s.Status == "Skipped")} -</span>
            </div>");

            html.AppendLine("<div class='steps-container'>");

            foreach (var step in steps)
            {
                var statusClass = step.Status.ToLower();

                html.AppendLine($@"
                <div class='step {statusClass}'>
                    <div class='step-header' data-expandable>
                        <span class='step-icon'>{GetStatusIcon(step.Status)}</span>
                        <div class='step-text'>
                            <div>
                                <span class='step-definition'>{step.StepDefinition}</span>
                                <span class='step-name'>{step.StepName}</span>
                            </div>
                            {(step.Status == "Failed" ? $"<div class='step-error'>{step.ErrorMessage}</div>" : "")}
                        </div>
                        <span class='step-duration'>{step.Duration}ms</span>
                        <span class='step-toggle'>▼</span>
                    </div>
                    <div class='step-details'>
                        {string.Join("", step.Details.Select(d => $"<div>{HtmlEncoder.Default.Encode(d)}</div>"))}
                    </div>
                </div>");
            }

            html.AppendLine("</div></div>");

            html.AppendLine(@"
            <script>
                document.querySelectorAll('.step-header').forEach(header => {
                    header.addEventListener('click', function () {
                        const step = this.closest('.step');
                        const isOpen = step.classList.contains('open');

                        document.querySelectorAll('.step').forEach(s => {
                            s.classList.remove('open');
                            const toggle = s.querySelector('.step-toggle');
                            if (toggle) toggle.textContent = '▼';
                        });

                        if (!isOpen) {
                            step.classList.add('open');
                            const toggle = step.querySelector('.step-toggle');
                            if (toggle) toggle.textContent = '▲';
                        }
                    });
                });
            </script>
            ");

            return html.ToString();
        }

        public static List<TestStep> ParseStdOutFromXml(string xmlFilePath)
        {
            var steps = new List<TestStep>();
            var xmlDoc = XDocument.Load(xmlFilePath);
            var ns = XNamespace.Get("http://microsoft.com/schemas/VisualStudio/TeamTest/2010");

            var unitTestStdOut = xmlDoc.Descendants(ns + "UnitTestResult")
                                     .Elements(ns + "Output")
                                     .Elements(ns + "StdOut")
                                     .FirstOrDefault()?.Value;

            var resultSummaryStdOut = xmlDoc.Descendants(ns + "ResultSummary")
                                          .Elements(ns + "Output")
                                          .Elements(ns + "StdOut")
                                          .FirstOrDefault()?.Value;

            var outputContent = !string.IsNullOrEmpty(unitTestStdOut) && unitTestStdOut.Contains("Given") 
                              ? unitTestStdOut 
                              : resultSummaryStdOut;

            if (string.IsNullOrEmpty(outputContent))
                return steps;

            var cleanedContent = outputContent.Replace("&#xD;", "").Replace("&#xA;", "\n");
            var lines = cleanedContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            TestStep currentStep = null;
            var durationRegex = new Regex(@"\((\d+\.\d+)s\)");
            bool inOutputSection = outputContent == resultSummaryStdOut;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                if (inOutputSection)
                {
                    if (line.EndsWith("Output:") && line.Contains("xUnit.net"))
                    {
                        continue;
                    }
                    else if (line.StartsWith("Finished:") && line.Contains("Cubic.Aeris.Test"))
                    {
                        break;
                    }
                }

                if (line.StartsWith("Given ") || line.StartsWith("When ") || 
                    line.StartsWith("Then ") || line.StartsWith("And ") || line.StartsWith("But "))
                {
                    if (currentStep != null)
                    {
                        CheckStepStatusFromNextLines(lines, i, currentStep);
                        steps.Add(currentStep);
                    }

                    currentStep = new TestStep
                    {
                        StepDefinition = line.Split(' ')[0].Trim(),
                        StepName = line.Substring(line.IndexOf(' ') + 1).Trim(),
                        Status = "Passed",
                        Details = new List<string>(),
                        Parameters = new Dictionary<string, string>(),
                        StartTime = DateTime.Now
                    };
                }
                else if (currentStep != null)
                {
                    currentStep.Details.Add(line);

                    ExtractSpecialInfo(line, currentStep);

                    ExtractDurationInfo(line, currentStep, durationRegex);
                }
            }

            if (currentStep != null)
            {
                CheckStepStatusFromNextLines(lines, lines.Length, currentStep);
                steps.Add(currentStep);
            }

            return steps;
        }

        private static void CheckStepStatusFromNextLines(string[] lines, int currentIndex, TestStep step)
        {
            for (int i = currentIndex + 1; i < Math.Min(currentIndex + 5, lines.Length); i++)
            {
                var line = lines[i].Trim();
                if (line.StartsWith("-> error:") || line.StartsWith("-> fail:") || line.StartsWith("-> failure:"))
                {
                    step.Status = "Failed";
                    step.ErrorMessage = line;
                    break;
                }
                else if (line.StartsWith("-> skipped:"))
                {
                    step.Status = "Skipped";
                    break;
                }
            }
        }

        private static void ExtractSpecialInfo(string line, TestStep step)
        {
            if (line.Contains("TransactionId:"))
            {
                var parts = line.Split(new[] { "TransactionId:" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    step.Parameters["TransactionId"] = parts[1].Trim();
                }
            }
            else if (line.Contains("DataDog Logs:"))
            {
                var parts = line.Split(new[] { "DataDog Logs:" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    step.Parameters["DataDogLogs"] = parts[1].Trim();
                }
            }
        }

        private static void ExtractDurationInfo(string line, TestStep step, Regex durationRegex)
        {
            if (line.StartsWith("-> done:") || line.StartsWith("-> error:"))
            {
                var match = durationRegex.Match(line);
                if (match.Success && double.TryParse(match.Groups[1].Value, out var seconds))
                {
                    step.Duration = (long)(seconds * 1000);
                }
            }
        }

        public static string GetTestResultsFilePath(string buildedTestFileFullPath, string scenarioTag)
        {
            var testResultsFolder = Path.Combine(buildedTestFileFullPath, "TestResults");
            if (!Directory.Exists(testResultsFolder))
            {
                Directory.CreateDirectory(testResultsFolder);
            }

            var fileName = $"{scenarioTag}_{DateTime.Now:yyyyMMdd_HHmmss}_test_results.xml";
            return Path.Combine(testResultsFolder, fileName);
        }

        private static string GetStatusIcon(string status)
        {
            return status switch
            {
                "Failed" => @"<svg width='16' height='16' viewBox='0 0 24 24' stroke-width='2' stroke='#c62828' fill='none' stroke-linecap='round' stroke-linejoin='round'><path d='M18 6L6 18'/><path d='M6 6l12 12'/></svg>",
                "Skipped" => @"<svg width='16' height='16' viewBox='0 0 24 24' stroke-width='2' stroke='#e65100' fill='none' stroke-linecap='round' stroke-linejoin='round'><path d='M12 12m-9 0a9 9 0 1 0 18 0a9 9 0 1 0 -18 0'/><path d='M12 7v5l3 3'/></svg>",
                _ => @"<svg width='16' height='16' viewBox='0 0 24 24' stroke-width='2' stroke='#2e7d32' fill='none' stroke-linecap='round' stroke-linejoin='round'><path d='M5 12l5 5l10 -10'/></svg>"
            };
        }


        public static List<string> ListAllTests(string featureFilePath, SearchOption searchOption)
        {
            List<string> testCases = new List<string>();
            var featureFiles = Directory.GetFiles(featureFilePath, "*.feature", searchOption)
                .Where(file => !file.Contains("target", StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            foreach (var file in featureFiles)
            {
                var lines = File.ReadAllLines(file);

                foreach (var line in lines)
                {
                    if (line.Trim().StartsWith("Scenario:") || line.Trim().StartsWith("Scenario Outline:"))
                    {
                        testCases.Add(line.Trim());
                    }
                }
            }

            return testCases;
        }
        
        public static List<object> ProcessFeatureFiles(string projectPath, string projectName, string languageType, List<string> testCases)
        {
            var projectType = DetectProjectType(projectPath);
            var rootNode = new
            {
                id = "root",
                text = projectName,
                icon = GetRootIcon(languageType),
                state = new { opened = true },
                children = new List<object>()
            };

            var folderFeatureMap = new Dictionary<string, List<object>>();

            foreach (var file in Directory.GetFiles(projectPath, "*.feature", SearchOption.AllDirectories)
                .Where(file => languageType.ToLower() != "java" || !file.Contains("target", StringComparison.OrdinalIgnoreCase)))
            {
                bool isCommentedOut = true;
                using (var reader = new StreamReader(file))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#"))
                        {
                            isCommentedOut = false;
                            break;
                        }
                    }
                }

                if (isCommentedOut)
                {
                    continue;
                }

                var directoryName = Path.GetFileName(Path.GetDirectoryName(file)) ?? "Other";
                if (!folderFeatureMap.ContainsKey(directoryName))
                {
                    folderFeatureMap[directoryName] = new List<object>();
                }

                using (var reader = new StreamReader(file))
                {
                    var gherkinDocument = new Parser().Parse(reader);

                    var featureNode = new
                    {
                        id = file,
                        text = Path.GetFileNameWithoutExtension(file),
                        icon = GetFeatureIcon(projectType),
                        path = file,
                        children = gherkinDocument.Feature.Children.OfType<Scenario>()
                            .Select(scenario => new
                            {
                                id = $"{file}|{scenario.Name}",
                                text = scenario.Name,
                                icon = "/img/icons8-scenario-16.png",
                                path = file,
                                scenarioName = scenario.Name,
                                data = new
                                {
                                    fullName = testCases.FirstOrDefault(t => t.EndsWith($".{scenario.Name}")) ?? scenario.Name
                                }
                            }).ToList()
                    };

                    folderFeatureMap[directoryName].Add(featureNode);
                }
            }

            foreach (var folder in folderFeatureMap)
            {
                var folderNode = new
                {
                    id = $"folder_{folder.Key}",
                    text = folder.Key,
                    icon = GetRootIcon("folder"),
                    state = new { opened = false },
                    children = folder.Value
                };

                rootNode.children.Add(folderNode);
            }

            return new List<object> { rootNode };
        }

        public static string GetFeatureHeader(Feature feature)
        {
            var sb = new StringBuilder();
            
            if (feature.Tags != null)
            {
                sb.AppendLine(string.Join(" ", feature.Tags.Select(t => t.Name)));
            }
            
            sb.AppendLine($"Feature: {feature.Name}");
            return sb.ToString();
        }

        public static string GetBackgroundText(Feature feature)
        {
            var background = feature.Children.OfType<Background>().FirstOrDefault();
            if (background == null) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine($"{background.Keyword}: {background.Name}");
            foreach (var step in background.Steps)
            {
                sb.AppendLine($"  {step.Keyword}{step.Text}");
            }
            return sb.ToString();
        }

        public static string GetScenarioText(Scenario scenario)
        {
            var sb = new StringBuilder();
    
            if (scenario.Tags != null)
            {
                sb.AppendLine(string.Join(" ", scenario.Tags.Select(t => t.Name)));
            }
            
            sb.AppendLine($"{scenario.Keyword}: {scenario.Name}");

            foreach (var step in scenario.Steps)
            {
                sb.AppendLine($"  {step.Keyword}{step.Text}");

                if (step.Argument is Gherkin.Ast.DataTable dataTable)
                {
                    foreach (var row in dataTable.Rows)
                    {
                        sb.AppendLine($"    | {string.Join(" | ", row.Cells.Select(c => c.Value))} |");
                    }
                }
            }

            if (scenario.Examples != null)
            {
                foreach (var example in scenario.Examples)
                {
                    sb.AppendLine();
                    sb.AppendLine($"  {example.Keyword}: {example.Name}");
                    sb.AppendLine($"    | {string.Join(" | ", example.TableHeader.Cells.Select(c => c.Value))} |");
                    foreach (var row in example.TableBody)
                    {
                        sb.AppendLine($"    | {string.Join(" | ", row.Cells.Select(c => c.Value))} |");
                    }
                }
            }

            return sb.ToString();
        }

        public static string DetectProjectType(string projectPath)
        {
            if (Directory.GetFiles(projectPath, "pom.xml", SearchOption.AllDirectories).Any())
                return "java";
            else
            {
                var csprojFiles = Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories);
            
                return DetermineFrameworkBasedOnCount(csprojFiles);
            }
        }

        private static string DetermineFrameworkBasedOnCount(string[] csprojFiles)
        {
            int specflowCount = 0;
            int reqnrollCount = 0;

            foreach (var csproj in csprojFiles)
            {
                var content = System.IO.File.ReadAllText(csproj);

                specflowCount += CountOccurrences(content, "SpecFlow.");
                reqnrollCount += CountOccurrences(content, "Reqnroll.");
            }

            if (specflowCount > reqnrollCount)
            {
                return "specflow";
            }
            else if (reqnrollCount > specflowCount)
            {
                return "reqnroll";
            }
            else
            {
                return "none";
            }
        }

        private static int CountOccurrences(string text, string word)
        {
            int count = 0;
            int index = 0;

            while ((index = text.IndexOf(word, index, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                count++;
                index += word.Length;
            }

            return count;
        }

        private static string GetRootIcon(string languageType) => languageType switch
        {
            "csharp" => "/img/icons8-c-24.png",
            "java" => "/img/icons8-java-24.png",
            _ => "/img/open-folder-24.png"
        };

        private static string GetFeatureIcon(string projectType) => projectType switch
        {
            "specflow" => "/img/icons8-specflow-12.png",
            "reqnroll" => "/img/icons8-reqnroll-12.png",
            "java" => "/img/icons8-cucumber-12.png",
            _ => "jstree-file"
        };
    }
}
