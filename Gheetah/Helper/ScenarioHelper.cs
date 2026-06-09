using Gheetah.Models.ScenarioModel;
using Gherkin;
using Gherkin.Ast;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
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
                        SetStepStatusFromDetails(currentStep);
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
                SetStepStatusFromDetails(currentStep);
                steps.Add(currentStep);
            }

            return steps;
        }

        private static void SetStepStatusFromDetails(TestStep step)
        {
            foreach (var detail in step.Details)
            {
                var d = detail.Trim();
                if (d.StartsWith("-> error:") || d.StartsWith("-> fail:") || d.StartsWith("-> failure:"))
                {
                    step.Status = "Failed";
                    if (string.IsNullOrEmpty(step.ErrorMessage)) step.ErrorMessage = d;
                    return;
                }
                if (d.StartsWith("-> skipped because of previous errors"))
                {
                    step.Status = "Failed";
                    return;
                }
                if (d.StartsWith("-> No matching step definition"))
                {
                    step.Status = "Failed";
                    if (string.IsNullOrEmpty(step.ErrorMessage)) step.ErrorMessage = "No matching step definition found";
                    return;
                }
                if (d.StartsWith("-> skipped"))
                {
                    step.Status = "Skipped";
                    return;
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


        public static string GeneratePlaywrightHtmlReport(string jsonFilePath)
        {
            try
            {
                var json = File.ReadAllText(jsonFilePath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                int passed = 0, failed = 0, skipped = 0;
                double totalDurationMs = 0;

                if (root.TryGetProperty("stats", out var stats))
                {
                    passed = stats.TryGetProperty("expected", out var ep) ? ep.GetInt32() : 0;
                    failed = stats.TryGetProperty("unexpected", out var up) ? up.GetInt32() : 0;
                    skipped = stats.TryGetProperty("skipped", out var sk) ? sk.GetInt32() : 0;
                    totalDurationMs = stats.TryGetProperty("duration", out var dur) ? dur.GetDouble() : 0;
                }

                var html = new StringBuilder();

                html.AppendLine(@"<style>
.pw-report{font-family:system-ui,-apple-system,sans-serif;font-size:14px;}
.pw-summary{display:flex;align-items:center;gap:12px;padding:10px 0 14px;margin-bottom:14px;border-bottom:1px solid #e2e8f0;flex-wrap:wrap;}
.pw-badge{padding:4px 10px;border-radius:12px;font-size:12px;font-weight:600;}
.pw-badge.pw-pass{background:#dcfce7;color:#16a34a;}
.pw-badge.pw-fail{background:#fee2e2;color:#dc2626;}
.pw-badge.pw-skip{background:#fef9c3;color:#ca8a04;}
.pw-total-time{color:#64748b;font-size:12px;margin-left:auto;}
.pw-suite{margin-bottom:10px;border:1px solid #e2e8f0;border-radius:8px;overflow:hidden;}
.pw-suite-title{background:#f8fafc;padding:7px 14px;font-size:12px;font-weight:600;color:#475569;border-bottom:1px solid #e2e8f0;font-family:monospace;letter-spacing:.02em;}
.pw-spec{padding:10px 14px;border-bottom:1px solid #f1f5f9;transition:background .15s;}
.pw-spec:last-child{border-bottom:none;}
.pw-spec.pw-fail{cursor:pointer;}
.pw-spec.pw-fail:hover{background:#fff8f8;}
.pw-spec-row{display:flex;align-items:center;gap:10px;}
.pw-icon{flex-shrink:0;font-weight:700;font-size:15px;width:18px;text-align:center;}
.pw-pass .pw-icon{color:#16a34a;}
.pw-fail .pw-icon{color:#dc2626;}
.pw-skip .pw-icon{color:#ca8a04;}
.pw-spec-name{flex-grow:1;color:#1e293b;font-weight:500;}
.pw-fail .pw-spec-name{color:#dc2626;}
.pw-browser{font-size:11px;color:#94a3b8;background:#f1f5f9;padding:1px 6px;border-radius:4px;white-space:nowrap;}
.pw-dur{font-size:12px;color:#94a3b8;min-width:48px;text-align:right;white-space:nowrap;}
.pw-toggle{color:#94a3b8;font-size:11px;margin-left:4px;transition:transform .2s;}
.pw-spec.open .pw-toggle{transform:rotate(180deg);}
.pw-error{display:none;margin-top:10px;padding:10px 12px;background:#fff5f5;border:1px solid #fecaca;border-radius:6px;font-family:monospace;font-size:12px;color:#7f1d1d;white-space:pre-wrap;overflow-x:auto;line-height:1.5;}
.pw-attachments{display:none;margin-top:10px;display:flex;flex-wrap:wrap;gap:12px;}
.pw-spec.open .pw-error,.pw-spec.open .pw-attachments{display:flex;}
.pw-spec.open .pw-error{display:block;}
.pw-attachment{flex:1;min-width:220px;}
.pw-att-label{font-size:11px;font-weight:600;color:#64748b;text-transform:uppercase;letter-spacing:.04em;margin-bottom:5px;}
.pw-screenshot{max-width:100%;border-radius:6px;border:1px solid #e2e8f0;display:block;cursor:zoom-in;}
.pw-video{max-width:100%;border-radius:6px;display:block;}
</style>");

                html.AppendLine("<div class='pw-report'>");

                var durationLabel = totalDurationMs >= 1000
                    ? $"{totalDurationMs / 1000.0:F1}s"
                    : $"{totalDurationMs:F0}ms";

                html.Append("<div class='pw-summary'>");
                html.Append($"<span class='pw-badge pw-pass'>✓ {passed} passed</span>");
                if (failed > 0) html.Append($"<span class='pw-badge pw-fail'>✗ {failed} failed</span>");
                if (skipped > 0) html.Append($"<span class='pw-badge pw-skip'>− {skipped} skipped</span>");
                html.Append($"<span class='pw-total-time'>{durationLabel}</span>");
                html.AppendLine("</div>");

                if (root.TryGetProperty("suites", out var suites))
                    BuildPlaywrightSuitesHtml(suites, html);

                html.AppendLine("</div>");

                html.AppendLine(@"<script>
document.querySelectorAll('.pw-spec').forEach(function(el){
  var toggle=el.querySelector('.pw-toggle');
  if(!toggle) return;
  el.style.cursor='pointer';
  el.addEventListener('click',function(e){
    if(e.target.tagName==='VIDEO'||e.target.tagName==='IMG'||e.target.tagName==='SOURCE') return;
    this.classList.toggle('open');
    var t=this.querySelector('.pw-toggle');
    if(t) t.textContent=this.classList.contains('open')?'▲':'▼';
  });
  if(el.classList.contains('open')) toggle.textContent='▲';
});
</script>");

                return html.ToString();
            }
            catch (Exception ex)
            {
                return $"<div style='color:#dc2626;padding:10px;'>Failed to generate Playwright report: {HtmlEncoder.Default.Encode(ex.Message)}</div>";
            }
        }

        private static void BuildPlaywrightSuitesHtml(JsonElement suites, StringBuilder html)
        {
            foreach (var suite in suites.EnumerateArray())
            {
                if (suite.TryGetProperty("suites", out var nested))
                    BuildPlaywrightSuitesHtml(nested, html);

                if (!suite.TryGetProperty("specs", out var specs) || specs.GetArrayLength() == 0)
                    continue;

                var title = suite.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";

                html.AppendLine("<div class='pw-suite'>");
                if (!string.IsNullOrEmpty(title))
                    html.AppendLine($"<div class='pw-suite-title'>{HtmlEncoder.Default.Encode(title)}</div>");

                foreach (var spec in specs.EnumerateArray())
                {
                    var specTitle = spec.TryGetProperty("title", out var st) ? st.GetString() ?? "Unknown Test" : "Unknown Test";
                    var ok = spec.TryGetProperty("ok", out var okProp) && okProp.GetBoolean();

                    string statusClass = ok ? "pw-pass" : "pw-fail";
                    string statusIcon = ok ? "✓" : "✗";
                    string browser = "";
                    string errorText = "";
                    long duration = 0;

                    var attachmentHtml = new StringBuilder();

                    if (spec.TryGetProperty("tests", out var tests))
                    {
                        foreach (var test in tests.EnumerateArray())
                        {
                            if (string.IsNullOrEmpty(browser) && test.TryGetProperty("projectName", out var pn))
                                browser = pn.GetString() ?? "";

                            if (test.TryGetProperty("results", out var results))
                            {
                                foreach (var result in results.EnumerateArray())
                                {
                                    if (result.TryGetProperty("status", out var s) && s.GetString() == "skipped")
                                    {
                                        statusClass = "pw-skip";
                                        statusIcon = "−";
                                    }
                                    if (result.TryGetProperty("duration", out var dur))
                                        duration = dur.TryGetInt64(out var d) ? d : 0;

                                    if (string.IsNullOrEmpty(errorText) && result.TryGetProperty("errors", out var errors))
                                    {
                                        var parts = new List<string>();
                                        foreach (var err in errors.EnumerateArray())
                                        {
                                            if (err.TryGetProperty("message", out var msg))
                                                parts.Add(msg.GetString() ?? "");
                                        }
                                        errorText = string.Join("\n\n", parts);
                                    }

                                    if (result.TryGetProperty("attachments", out var attachments))
                                    {
                                        foreach (var att in attachments.EnumerateArray())
                                        {
                                            var attName = att.TryGetProperty("name", out var an) ? an.GetString() ?? "" : "";
                                            var attPath = att.TryGetProperty("path", out var ap) ? ap.GetString() ?? "" : "";
                                            var attType = att.TryGetProperty("contentType", out var ac) ? ac.GetString() ?? "" : "";

                                            if (string.IsNullOrEmpty(attPath) || !File.Exists(attPath)) continue;

                                            try
                                            {
                                                if (attType is "image/png" or "image/jpeg")
                                                {
                                                    var bytes = File.ReadAllBytes(attPath);
                                                    var b64 = Convert.ToBase64String(bytes);
                                                    attachmentHtml.Append($"<div class='pw-attachment'><div class='pw-att-label'>{HtmlEncoder.Default.Encode(attName)}</div><img src='data:{attType};base64,{b64}' class='pw-screenshot' onclick=\"this.style.maxWidth=this.style.maxWidth==='none'?'100%':'none'\"/></div>");
                                                }
                                                else if (attType is "video/webm" or "video/mp4")
                                                {
                                                    var encodedPath = Uri.EscapeDataString(attPath);
                                                    attachmentHtml.Append($"<div class='pw-attachment'><div class='pw-att-label'>{HtmlEncoder.Default.Encode(attName)}</div><video controls class='pw-video'><source src='/Scenarios/GetPlaywrightAttachment?filePath={encodedPath}' type='{attType}'/></video></div>");
                                                }
                                            }
                                            catch { /* skip unreadable attachments */ }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    var durLabel = duration >= 1000 ? $"{duration / 1000.0:F1}s" : $"{duration}ms";
                    var hasContent = !string.IsNullOrEmpty(errorText) || attachmentHtml.Length > 0;
                    var autoOpen = statusClass == "pw-fail" && hasContent ? " open" : "";
                    var toggleHtml = hasContent ? "<span class='pw-toggle'>▼</span>" : "";
                    var browserHtml = !string.IsNullOrEmpty(browser)
                        ? $"<span class='pw-browser'>{HtmlEncoder.Default.Encode(browser)}</span>" : "";
                    var errorHtml = !string.IsNullOrEmpty(errorText)
                        ? $"<div class='pw-error'>{HtmlEncoder.Default.Encode(errorText)}</div>" : "";
                    var attSectionHtml = attachmentHtml.Length > 0
                        ? $"<div class='pw-attachments'>{attachmentHtml}</div>" : "";

                    html.AppendLine($@"<div class='pw-spec {statusClass}{autoOpen}'>
  <div class='pw-spec-row'>
    <span class='pw-icon'>{statusIcon}</span>
    <span class='pw-spec-name'>{HtmlEncoder.Default.Encode(specTitle)}</span>
    {browserHtml}
    <span class='pw-dur'>{durLabel}</span>
    {toggleHtml}
  </div>
  {errorHtml}
  {attSectionHtml}
</div>");
                }

                html.AppendLine("</div>");
            }
        }

        public static string GenerateCucumberHtmlReport(string jsonFilePath)
        {
            try
            {
                var json = File.ReadAllText(jsonFilePath);
                if (string.IsNullOrWhiteSpace(json)) return "";
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Array) return "";

                var html = new StringBuilder();

                html.AppendLine(@"<style>
.cu-report{font-family:system-ui,-apple-system,sans-serif;font-size:14px;}
.cu-feature-title{font-size:11px;font-weight:700;color:#475569;text-transform:uppercase;letter-spacing:.06em;margin-bottom:8px;padding:6px 12px;background:#f8fafc;border-radius:6px;}
.cu-scenario{border:1px solid #e2e8f0;border-radius:8px;overflow:hidden;margin-bottom:10px;}
.cu-scenario-header{display:flex;align-items:center;gap:10px;padding:9px 14px;background:#fafafa;border-bottom:1px solid #f1f5f9;}
.cu-scenario-name{flex-grow:1;font-weight:600;color:#1e293b;}
.cu-scenario-badge{font-size:11px;font-weight:700;padding:2px 8px;border-radius:10px;}
.cu-scenario-badge.passed{background:#dcfce7;color:#16a34a;}
.cu-scenario-badge.failed{background:#fee2e2;color:#dc2626;}
.cu-steps{padding:6px 0;}
.cu-step{display:flex;align-items:flex-start;gap:8px;padding:5px 14px;border-bottom:1px solid #f8fafc;}
.cu-step:last-child{border-bottom:none;}
.cu-step.failed{background:#fff8f8;}
.cu-step.skipped,.cu-step.undefined{opacity:.65;}
.cu-step-icon{flex-shrink:0;font-weight:700;width:16px;text-align:center;margin-top:1px;font-size:13px;}
.cu-step.passed .cu-step-icon{color:#16a34a;}
.cu-step.failed .cu-step-icon{color:#dc2626;}
.cu-step.skipped .cu-step-icon{color:#94a3b8;}
.cu-step.undefined .cu-step-icon{color:#ca8a04;}
.cu-step.pending .cu-step-icon{color:#ca8a04;}
.cu-step-kw{font-weight:600;color:#6366f1;}
.cu-step-text{color:#334155;flex-grow:1;}
.cu-step-dur{font-size:11px;color:#94a3b8;white-space:nowrap;margin-left:8px;}
.cu-step-error{margin-top:6px;padding:8px 10px;background:#fff5f5;border:1px solid #fecaca;border-radius:4px;font-family:monospace;font-size:11px;color:#7f1d1d;white-space:pre-wrap;overflow-x:auto;max-height:220px;overflow-y:auto;line-height:1.4;}
</style>");

                html.AppendLine("<div class='cu-report'>");

                foreach (var feature in doc.RootElement.EnumerateArray())
                {
                    var featureName = feature.TryGetProperty("name", out var fn) ? fn.GetString() ?? "" : "";
                    if (!feature.TryGetProperty("elements", out var elements)) continue;

                    html.AppendLine("<div class='cu-feature'>");
                    if (!string.IsNullOrEmpty(featureName))
                        html.AppendLine($"<div class='cu-feature-title'>Feature: {HtmlEncoder.Default.Encode(featureName)}</div>");

                    foreach (var element in elements.EnumerateArray())
                    {
                        var keyword = element.TryGetProperty("keyword", out var ek) ? ek.GetString()?.Trim() ?? "" : "";
                        if (keyword.Equals("Background", StringComparison.OrdinalIgnoreCase)) continue;

                        var scenarioName = element.TryGetProperty("name", out var sn) ? sn.GetString() ?? "" : "";
                        bool scenarioPassed = true;
                        var stepsHtml = new StringBuilder();

                        if (element.TryGetProperty("steps", out var steps))
                        {
                            foreach (var step in steps.EnumerateArray())
                            {
                                var kw = step.TryGetProperty("keyword", out var kwp) ? kwp.GetString() ?? "" : "";
                                var stepName = step.TryGetProperty("name", out var stn) ? stn.GetString() ?? "" : "";

                                string status = "passed";
                                string errorMsg = "";
                                long durationNs = 0;

                                if (step.TryGetProperty("result", out var result))
                                {
                                    if (result.TryGetProperty("status", out var st)) status = st.GetString() ?? "passed";
                                    if (result.TryGetProperty("duration", out var dur)) durationNs = dur.TryGetInt64(out var d) ? d : 0;
                                    if (result.TryGetProperty("error_message", out var em)) errorMsg = em.GetString() ?? "";
                                }

                                if (status == "failed") scenarioPassed = false;

                                var durationMs = durationNs / 1_000_000L;
                                var durLabel = durationMs >= 1000 ? $"{durationMs / 1000.0:F1}s" : $"{durationMs}ms";
                                var icon = status switch { "passed" => "✓", "failed" => "✗", "skipped" => "−", "undefined" => "?", _ => "·" };
                                var errorHtml = !string.IsNullOrEmpty(errorMsg)
                                    ? $"<div class='cu-step-error'>{HtmlEncoder.Default.Encode(errorMsg)}</div>" : "";

                                stepsHtml.AppendLine($@"<div class='cu-step {status}'>
  <span class='cu-step-icon'>{icon}</span>
  <span class='cu-step-text'><span class='cu-step-kw'>{HtmlEncoder.Default.Encode(kw.TrimEnd())}</span> {HtmlEncoder.Default.Encode(stepName)}</span>
  <span class='cu-step-dur'>{(durationMs > 0 ? durLabel : "")}</span>
</div>{(errorHtml.Length > 0 ? $"<div style='padding:0 14px 6px;'>{errorHtml}</div>" : "")}");
                            }
                        }

                        var statusLabel = scenarioPassed ? "passed" : "failed";
                        html.AppendLine($@"<div class='cu-scenario'>
  <div class='cu-scenario-header'>
    <span class='cu-scenario-name'>{HtmlEncoder.Default.Encode(scenarioName)}</span>
    <span class='cu-scenario-badge {statusLabel}'>{(scenarioPassed ? "PASSED" : "FAILED")}</span>
  </div>
  <div class='cu-steps'>{stepsHtml}</div>
</div>");
                    }

                    html.AppendLine("</div>");
                }

                html.AppendLine("</div>");
                return html.ToString();
            }
            catch (Exception ex)
            {
                return $"<div style='color:#dc2626;padding:10px;'>Failed to generate Cucumber report: {HtmlEncoder.Default.Encode(ex.Message)}</div>";
            }
        }

        public static IEnumerable<string> FindCucumberJsonReports(string projectDir)
        {
            var reportsDir = Path.Combine(projectDir, "target", "cucumber-reports");
            if (!Directory.Exists(reportsDir)) return Enumerable.Empty<string>();
            return Directory.GetFiles(reportsDir, "*.json", SearchOption.AllDirectories)
                .OrderByDescending(f => new FileInfo(f).LastWriteTime);
        }

        public static List<string> ListPlaywrightTests(string testsPath, SearchOption searchOption)
        {
            var testNames = new List<string>();
            var specFiles = Directory.GetFiles(testsPath, "*.spec.ts", searchOption);
            foreach (var file in specFiles)
            {
                var content = File.ReadAllText(file);
                var matches = Regex.Matches(content, @"test\s*\(\s*['""](.+?)['""]");
                foreach (Match match in matches)
                {
                    testNames.Add(match.Groups[1].Value);
                }
            }
            return testNames;
        }

        public static List<TestStep> ParsePlaywrightJsonResults(string jsonFilePath)
        {
            var steps = new List<TestStep>();
            try
            {
                var json = File.ReadAllText(jsonFilePath);
                using var doc = JsonDocument.Parse(json);
                TraversePlaywrightSuites(doc.RootElement, steps);
            }
            catch (Exception ex)
            {
                steps.Add(new TestStep
                {
                    StepDefinition = "Error",
                    StepName = $"Failed to parse Playwright results: {ex.Message}",
                    Status = "Failed",
                    Details = new List<string>(),
                    Parameters = new Dictionary<string, string>()
                });
            }
            return steps;
        }

        private static void TraversePlaywrightSuites(JsonElement element, List<TestStep> steps)
        {
            if (element.TryGetProperty("suites", out var suites))
            {
                foreach (var suite in suites.EnumerateArray())
                {
                    TraversePlaywrightSuites(suite, steps);
                }
            }

            if (element.TryGetProperty("specs", out var specs))
            {
                foreach (var spec in specs.EnumerateArray())
                {
                    var title = spec.TryGetProperty("title", out var t) ? t.GetString() ?? "Unknown Test" : "Unknown Test";
                    var ok = spec.TryGetProperty("ok", out var okProp) && okProp.GetBoolean();

                    string status = ok ? "Passed" : "Failed";
                    string errorMessage = null;
                    long duration = 0;

                    if (spec.TryGetProperty("tests", out var tests))
                    {
                        foreach (var test in tests.EnumerateArray())
                        {
                            if (test.TryGetProperty("results", out var results))
                            {
                                foreach (var result in results.EnumerateArray())
                                {
                                    if (result.TryGetProperty("status", out var s))
                                    {
                                        var rs = s.GetString();
                                        if (rs == "failed" || rs == "timedOut")
                                            status = "Failed";
                                        else if (rs == "skipped" && status != "Failed")
                                            status = "Skipped";
                                    }
                                    if (result.TryGetProperty("duration", out var dur))
                                    {
                                        duration = dur.TryGetInt64(out var d) ? d : 0;
                                    }
                                    if (errorMessage == null && result.TryGetProperty("errors", out var errors))
                                    {
                                        foreach (var err in errors.EnumerateArray())
                                        {
                                            if (err.TryGetProperty("message", out var msg))
                                            {
                                                errorMessage = msg.GetString();
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    steps.Add(new TestStep
                    {
                        StepDefinition = "Test",
                        StepName = title,
                        Status = status,
                        Duration = duration,
                        ErrorMessage = errorMessage,
                        Details = new List<string>(),
                        Parameters = new Dictionary<string, string>()
                    });
                }
            }
        }

        public static List<object> ProcessPlaywrightTests(string projectPath, string projectName, string testsPath)
        {
            var rootNode = new
            {
                id = "root",
                text = projectName,
                icon = "/img/icons8-playwright-24.png",
                state = new { opened = true },
                children = new List<object>()
            };

            // Defensive check — caller already validates, but guard against malformed ProjectInfo
            if (string.IsNullOrWhiteSpace(testsPath) || !Directory.Exists(testsPath))
                return new List<object> { rootNode };

            var specFiles = Directory.GetFiles(testsPath, "*.spec.ts", SearchOption.AllDirectories);
            var folderTestMap = new Dictionary<string, List<object>>();

            foreach (var file in specFiles)
            {
                var directoryName = Path.GetFileName(Path.GetDirectoryName(file)) ?? "tests";
                if (!folderTestMap.ContainsKey(directoryName))
                    folderTestMap[directoryName] = new List<object>();

                var structure = ParsePlaywrightTestStructure(file);
                var fileChildren = new List<object>();

                foreach (var (describeName, tests) in structure)
                {
                    if (describeName == null)
                    {
                        // Standalone tests (not inside a describe block)
                        foreach (var testName in tests)
                        {
                            fileChildren.Add((object)new
                            {
                                id = $"{file}|{testName}",
                                text = testName,
                                icon = "/img/icons8-scenario-16.png",
                                path = file,
                                scenarioName = testName,
                                data = new { fullName = testName }
                            });
                        }
                    }
                    else
                    {
                        // Tests inside a test.describe block — show describe as folder
                        var describeChildren = tests.Select(testName => (object)new
                        {
                            id = $"{file}|{describeName}|{testName}",
                            text = testName,
                            icon = "/img/icons8-scenario-16.png",
                            path = file,
                            scenarioName = testName,
                            data = new { fullName = $"{describeName} > {testName}" }
                        }).ToList();

                        fileChildren.Add((object)new
                        {
                            id = $"{file}|describe|{describeName}",
                            text = describeName,
                            icon = "ti ti-layout-collage",
                            path = file,
                            state = new { opened = false },
                            children = describeChildren
                        });
                    }
                }

                var fileNode = new
                {
                    id = file,
                    text = Path.GetFileNameWithoutExtension(file),
                    icon = "jstree-file",
                    path = file,
                    state = new { opened = true },
                    children = fileChildren
                };

                folderTestMap[directoryName].Add(fileNode);
            }

            foreach (var folder in folderTestMap)
            {
                var folderNode = new
                {
                    id = $"folder_{folder.Key}",
                    text = folder.Key,
                    icon = GetRootIcon("folder"),
                    state = new { opened = true },
                    children = folder.Value
                };
                rootNode.children.Add(folderNode);
            }

            return new List<object> { rootNode };
        }

        private static List<(string describeName, List<string> tests)> ParsePlaywrightTestStructure(string filePath)
        {
            var content = File.ReadAllText(filePath);
            var result = new List<(string, List<string>)>();
            var coveredRanges = new List<(int start, int end)>();

            // Find all test.describe('Name', () => { ... }) blocks
            var describePattern = new Regex(
                @"test\.describe\s*\(\s*['""](.+?)['""]\s*,\s*\(\s*\)\s*=>\s*\{",
                RegexOptions.Singleline);

            foreach (Match dm in describePattern.Matches(content))
            {
                var describeName = dm.Groups[1].Value;
                var openBrace = content.LastIndexOf('{', dm.Index + dm.Length);
                if (openBrace < 0) continue;

                var closeBrace = FindMatchingBrace(content, openBrace);
                if (closeBrace < 0) continue;

                var blockContent = content.Substring(openBrace + 1, closeBrace - openBrace - 1);

                // Find direct test(...) calls inside this describe block
                var testPattern = new Regex(@"^\s*test\s*\(\s*['""](.+?)['""]\s*,\s*async", RegexOptions.Multiline);
                var tests = testPattern.Matches(blockContent)
                    .Cast<Match>()
                    .Select(m => m.Groups[1].Value)
                    .ToList();

                if (tests.Any())
                {
                    result.Add((describeName, tests));
                    coveredRanges.Add((dm.Index, closeBrace));
                }
            }

            // Find top-level standalone tests (not inside any describe block)
            var standalonePattern = new Regex(@"^test\s*\(\s*['""](.+?)['""]\s*,\s*async", RegexOptions.Multiline);
            var standalone = new List<string>();

            foreach (Match tm in standalonePattern.Matches(content))
            {
                bool inside = coveredRanges.Any(r => tm.Index >= r.start && tm.Index <= r.end);
                if (!inside)
                    standalone.Add(tm.Groups[1].Value);
            }

            if (standalone.Any())
                result.Insert(0, (null, standalone));

            return result;
        }

        private static int FindMatchingBrace(string content, int openBraceIndex)
        {
            int depth = 0;
            bool inString = false;
            char strChar = '"';
            bool inLineComment = false;
            bool inBlockComment = false;

            for (int i = openBraceIndex; i < content.Length; i++)
            {
                char c = content[i];

                if (inBlockComment)
                {
                    if (c == '*' && i + 1 < content.Length && content[i + 1] == '/') { inBlockComment = false; i++; }
                    continue;
                }
                if (inLineComment) { if (c == '\n') inLineComment = false; continue; }
                if (inString)
                {
                    if (c == '\\') { i++; continue; }
                    if (c == strChar) inString = false;
                    continue;
                }

                if (c == '/' && i + 1 < content.Length)
                {
                    if (content[i + 1] == '/') { inLineComment = true; continue; }
                    if (content[i + 1] == '*') { inBlockComment = true; continue; }
                }
                if (c == '"' || c == '\'' || c == '`') { inString = true; strChar = c; continue; }

                if (c == '{') depth++;
                else if (c == '}') { depth--; if (depth == 0) return i; }
            }
            return -1;
        }

        public static string ExtractPlaywrightTestContent(string filePath, string testName)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                var escapedName = Regex.Escape(testName);

                // Match the test call (ends at "async")
                var testPattern = new Regex(
                    $@"(\n|^)([ \t]*)test\s*\(\s*['""]({escapedName})['""]\s*,\s*async",
                    RegexOptions.Multiline);
                var match = testPattern.Match(content);
                if (!match.Success) return $"// test: {testName}\n// Content not found";

                var testStart = match.Index + match.Groups[1].Length; // skip leading newline

                // The signature looks like: async ({ page }) => {
                // We MUST find "=>" first to avoid picking up the "{" inside "({ page })"
                var searchFrom = match.Index + match.Length;
                var arrowIdx = content.IndexOf("=>", searchFrom);
                if (arrowIdx < 0) return $"// test: {testName}\n// Could not locate =>";

                // The function body { is the first { after =>
                var braceStart = content.IndexOf('{', arrowIdx + 2);
                if (braceStart < 0) return content;

                var braceEnd = FindMatchingBrace(content, braceStart);
                if (braceEnd < 0) return content;

                // After the closing } of the function body, include ); to close test(
                var testEnd = braceEnd + 1;
                var peek = testEnd;
                // Skip whitespace including newlines to reach ");
                while (peek < content.Length &&
                       (content[peek] == ' ' || content[peek] == '\t' ||
                        content[peek] == '\r' || content[peek] == '\n')) peek++;
                if (peek < content.Length && content[peek] == ')') { testEnd = peek + 1; }
                if (testEnd < content.Length && content[testEnd] == ';') testEnd++;

                var testBlock = content.Substring(testStart, testEnd - testStart).TrimEnd();

                // Prepend the describe block name as a comment (context)
                var describePattern = new Regex(
                    @"test\.describe\s*\(\s*['""](.+?)['""]\s*,\s*\(\s*\)\s*=>\s*\{",
                    RegexOptions.Singleline);
                string header = "";

                foreach (Match dm in describePattern.Matches(content))
                {
                    var openBrace = content.LastIndexOf('{', dm.Index + dm.Length);
                    if (openBrace < 0) continue;
                    var closeBrace = FindMatchingBrace(content, openBrace);
                    if (closeBrace >= 0 && match.Index > openBrace && match.Index < closeBrace)
                    {
                        header = $"// describe: {dm.Groups[1].Value}\n";
                        break;
                    }
                }

                return header + testBlock;
            }
            catch
            {
                return File.ReadAllText(filePath);
            }
        }

        private static List<string> ExtractPlaywrightTestNames(string specFilePath)
        {
            var testNames = new List<string>();
            var content = File.ReadAllText(specFilePath);
            var matches = Regex.Matches(content, @"test\s*\(\s*['""](.+?)['""]");
            foreach (Match match in matches)
                testNames.Add(match.Groups[1].Value);
            return testNames;
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
