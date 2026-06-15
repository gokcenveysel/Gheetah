using Gheetah.Interfaces;
using Gheetah.Models.AiModels;
using System.Text;
using System.Text.Json;

namespace Gheetah.Services
{
    public class PrePromptService : IPrePromptService
    {
        private readonly IFileService _fileService;
        private readonly IPromptValidator _validator;
        private const string FileName = "pre-prompts.json";

        public PrePromptService(IFileService fileService, IPromptValidator validator)
        {
            _fileService = fileService;
            _validator = validator;
        }

        public async Task<string> GetRawContentAsync(string prePromptId)
        {
            var prompts = await _fileService.LoadConfigAsync<Dictionary<string, string>>(FileName)
                ?? new Dictionary<string, string>();
            return prompts.TryGetValue(prePromptId, out var content) ? content : string.Empty;
        }

        public async Task<string> BuildPrePromptAsync(string prePromptId, Dictionary<string, string> context)
        {
            var template = await GetRawContentAsync(prePromptId);
            if (string.IsNullOrEmpty(template)) return string.Empty;

            foreach (var (key, val) in context)
                template = template.Replace($"{{{{{key}}}}}", val ?? string.Empty);

            return template;
        }

        public async Task SavePrePromptAsync(string prePromptId, string rawContent)
        {
            var prompts = await _fileService.LoadConfigAsync<Dictionary<string, string>>(FileName)
                ?? new Dictionary<string, string>();
            prompts[prePromptId] = rawContent;
            await _fileService.SaveConfigAsync(FileName, prompts);
        }

        public async Task<ValidationResult> ValidatePrePromptAsync(string content)
        {
            return await _validator.ValidateInputAsync(content, new ValidationContext
            {
                Source = "pre-prompt",
                MaxTokenBudget = 8000
            });
        }

        public string GenerateFromRequirements(List<string> testTypes, string targetUrl, Dictionary<string, string> req)
        {
            var sb = new StringBuilder();
            var appName = req.GetValueOrDefault("appName", "the application");
            var types = testTypes != null && testTypes.Count > 0
                ? string.Join(", ", testTypes)
                : "software quality assurance";

            sb.AppendLine($"You are an expert QA automation engineer specializing in {types}.");
            sb.AppendLine();
            sb.AppendLine("Your mission is to test the application described below using BDD methodology.");
            sb.AppendLine("Generate precise Gherkin scenarios and execute them methodically, one by one.");
            sb.AppendLine();
            sb.AppendLine("## Application Under Test");
            sb.AppendLine($"- **Name**: {appName}");
            if (!string.IsNullOrWhiteSpace(targetUrl))
                sb.AppendLine($"- **URL**: {targetUrl}");
            sb.AppendLine();

            foreach (var tt in (testTypes ?? new List<string>()))
            {
                switch (tt)
                {
                    case "UI Testing":      AppendUiSection(sb, req);     break;
                    case "API Testing":     AppendApiSection(sb, req);    break;
                    case "E2E Testing":     AppendE2eSection(sb, req);    break;
                    case "Regression":      AppendRegressionSection(sb, req); break;
                    case "Smoke Testing":   AppendSmokeSection(sb, req);  break;
                    case "Accessibility":   AppendA11ySection(sb, req);   break;
                }
            }

            var tags = (testTypes ?? new List<string>())
                .Select(t => "@" + t.Replace(" ", "").ToLower());
            sb.AppendLine("## Scenario Writing Standards");
            sb.AppendLine("- Use **Given/When/Then** format — every step must be unambiguous and self-contained.");
            sb.AppendLine("- Each scenario must be **atomic and independent** — no inter-scenario dependencies.");
            sb.AppendLine("- Write step names in plain English readable by any team member without technical background.");
            sb.AppendLine("- Cover both **happy path** and **negative cases** (invalid input, permission denied, boundary values).");
            sb.AppendLine($"- Tag each scenario appropriately: {string.Join(", ", tags)}, and one of @critical / @high / @medium / @low.");
            sb.AppendLine();
            sb.AppendLine("## Execution Rules");
            sb.AppendLine("- Take a screenshot after every significant user interaction or assertion point.");
            sb.AppendLine("- Log all network requests and responses when relevant to the scenario under test.");
            sb.AppendLine("- On unexpected error: capture full page state and error details, then continue with the next scenario.");
            sb.AppendLine("- Never hardcode credentials or test data — use the environment variables supplied in the project configuration.");
            sb.AppendLine("- If the UI does not respond within 10 seconds, treat it as a failure and log a performance warning.");
            sb.AppendLine("- After each scenario, verify the application is back in a clean state before the next run.");

            return sb.ToString();
        }

        private static void AppendUiSection(StringBuilder sb, Dictionary<string, string> req)
        {
            sb.AppendLine("## UI Testing");
            var userRoles = req.GetValueOrDefault("ui_userRoles");
            var keyPages = req.GetValueOrDefault("ui_keyPages");
            var authRequired = req.GetValueOrDefault("ui_authRequired", "No");
            var authDetails = req.GetValueOrDefault("ui_authDetails");

            if (!string.IsNullOrWhiteSpace(userRoles))
                sb.AppendLine($"- **User Roles / Personas**: {userRoles}");
            if (!string.IsNullOrWhiteSpace(keyPages))
                sb.AppendLine($"- **Key Pages / Features**: {keyPages}");
            if (authRequired != "No" && !string.IsNullOrWhiteSpace(authDetails))
            {
                sb.AppendLine($"- **Authentication**: {authRequired}");
                sb.AppendLine($"- **Credentials / Setup**: {authDetails}");
            }
            sb.AppendLine();
            sb.AppendLine("**UI checklist:**");
            sb.AppendLine("1. All specified pages load without console errors or broken assets.");
            sb.AppendLine("2. Form inputs accept valid data and reject invalid data with clear, user-friendly messages.");
            sb.AppendLine("3. Navigation flows follow the expected user journey without dead ends.");
            sb.AppendLine("4. Loading indicators appear for async operations; UI is not interactive during loading.");
            sb.AppendLine("5. Success/error feedback (toasts, banners, inline messages) appears at the correct moment.");
            sb.AppendLine("6. All interactive elements (buttons, links, toggles, modals) produce the correct outcome.");
            if (!string.IsNullOrWhiteSpace(userRoles))
                sb.AppendLine($"7. Role-based access: each role ({userRoles}) sees only the permitted UI elements and cannot access restricted areas.");
            sb.AppendLine();
        }

        private static void AppendApiSection(StringBuilder sb, Dictionary<string, string> req)
        {
            sb.AppendLine("## API Testing");
            var docUrl = req.GetValueOrDefault("api_docUrl");
            var authMethod = req.GetValueOrDefault("api_authMethod", "None");
            var authValue = req.GetValueOrDefault("api_authValue");
            var keyEndpoints = req.GetValueOrDefault("api_keyEndpoints");
            var expectedResponses = req.GetValueOrDefault("api_expectedResponses");

            if (!string.IsNullOrWhiteSpace(docUrl)) sb.AppendLine($"- **API Documentation**: {docUrl}");
            if (authMethod != "None") sb.AppendLine($"- **Authentication Method**: {authMethod}");
            if (!string.IsNullOrWhiteSpace(authValue)) sb.AppendLine($"- **Auth Credential**: {authValue}");
            if (!string.IsNullOrWhiteSpace(keyEndpoints)) sb.AppendLine($"- **Endpoints to Test**: {keyEndpoints}");
            if (!string.IsNullOrWhiteSpace(expectedResponses)) sb.AppendLine($"- **Expected Behaviors**: {expectedResponses}");
            sb.AppendLine();
            sb.AppendLine("**API checklist:**");
            sb.AppendLine("1. Correct HTTP status codes: 200/201 success, 400 bad input, 401/403 auth failures, 404 not found, 500 server error.");
            sb.AppendLine("2. Response body schema matches documentation — all required fields present with correct types and formats.");
            sb.AppendLine("3. Authentication boundary: unauthenticated requests → 401; insufficient permission → 403.");
            sb.AppendLine("4. Input validation: empty body, missing required fields, wrong types, out-of-range values → 400 with descriptive message.");
            sb.AppendLine("5. Idempotency: repeated identical POST requests must not create duplicates; repeated PUT/DELETE are safe.");
            sb.AppendLine("6. List endpoints: test pagination parameters (page, size, offset), sorting, and filtering.");
            sb.AppendLine("7. Edge input: empty strings, maximum-length strings, Unicode characters, special symbols (< > & \" ').");
            sb.AppendLine();
        }

        private static void AppendE2eSection(StringBuilder sb, Dictionary<string, string> req)
        {
            sb.AppendLine("## End-to-End Testing");
            var flows = req.GetValueOrDefault("e2e_userFlows");
            var credentials = req.GetValueOrDefault("e2e_testCredentials");
            var testData = req.GetValueOrDefault("e2e_testData");

            if (!string.IsNullOrWhiteSpace(flows))
            {
                sb.AppendLine("**User Flows to Test:**");
                sb.AppendLine(flows);
                sb.AppendLine();
            }
            if (!string.IsNullOrWhiteSpace(credentials))
                sb.AppendLine($"- **Test Account Credentials**: {credentials}");
            if (!string.IsNullOrWhiteSpace(testData))
                sb.AppendLine($"- **Preconditions / Test Data**: {testData}");
            sb.AppendLine();
            sb.AppendLine("**E2E checklist:**");
            sb.AppendLine("1. Execute each user flow completely from start to finish without shortcuts.");
            sb.AppendLine("2. Verify data entered at one step is correctly reflected in all subsequent steps.");
            sb.AppendLine("3. Test cross-page state persistence: browser back, refresh, and direct URL access.");
            sb.AppendLine("4. Confirm side-effect triggers fire at correct points (emails, notifications, balance changes).");
            sb.AppendLine("5. Test session handling: timeout behavior, concurrent tab/session conflicts, re-authentication.");
            sb.AppendLine("6. Repeat each flow twice to confirm determinism — no intermittent failures.");
            sb.AppendLine();
        }

        private static void AppendRegressionSection(StringBuilder sb, Dictionary<string, string> req)
        {
            sb.AppendLine("## Regression Testing");
            var criticalPaths = req.GetValueOrDefault("reg_criticalPaths");
            var recentChanges = req.GetValueOrDefault("reg_recentChanges");
            var knownIssues = req.GetValueOrDefault("reg_knownIssues");

            if (!string.IsNullOrWhiteSpace(criticalPaths)) sb.AppendLine($"- **Critical Paths**: {criticalPaths}");
            if (!string.IsNullOrWhiteSpace(recentChanges)) sb.AppendLine($"- **Recent Changes / Focus Areas**: {recentChanges}");
            if (!string.IsNullOrWhiteSpace(knownIssues)) sb.AppendLine($"- **Known Flaky Areas (skip / mark)**: {knownIssues}");
            sb.AppendLine();
            sb.AppendLine("**Regression checklist:**");
            sb.AppendLine("1. Confirm all critical paths listed above still behave exactly as in the last passing build.");
            sb.AppendLine("2. Pay extra attention to code areas adjacent to recent changes — side effects are the primary regression source.");
            sb.AppendLine("3. Verify that previously fixed bugs have not re-emerged (use historic bug titles as scenario names).");
            sb.AppendLine("4. Test all integration boundaries: every handoff between two systems or services.");
            sb.AppendLine("5. Mark known flaky tests @flaky with a comment — do not fail the build on them, but track them.");
            sb.AppendLine();
        }

        private static void AppendSmokeSection(StringBuilder sb, Dictionary<string, string> req)
        {
            sb.AppendLine("## Smoke Testing");
            var criticalChecks = req.GetValueOrDefault("smoke_criticalChecks");
            var maxResponse = req.GetValueOrDefault("smoke_maxResponseTime", "3000ms");

            if (!string.IsNullOrWhiteSpace(criticalChecks)) sb.AppendLine($"- **Critical Checks**: {criticalChecks}");
            sb.AppendLine($"- **Max Acceptable Response Time**: {maxResponse}");
            sb.AppendLine();
            sb.AppendLine("**Smoke checklist:**");
            sb.AppendLine("1. Application loads without 5xx errors on the main entry point.");
            sb.AppendLine("2. Home / landing page renders within the specified response time.");
            sb.AppendLine("3. Core navigation links are reachable — no 404s on primary routes.");
            sb.AppendLine("4. At least one successful authentication can be performed end-to-end.");
            sb.AppendLine("5. The primary CTA or core business feature is reachable and functional.");
            sb.AppendLine("6. No critical JavaScript console errors appear on initial page load.");
            sb.AppendLine("7. **STOP and escalate immediately** if any smoke check fails — do not proceed to further testing.");
            sb.AppendLine();
        }

        private static void AppendA11ySection(StringBuilder sb, Dictionary<string, string> req)
        {
            sb.AppendLine("## Accessibility Testing");
            var wcagLevel = req.GetValueOrDefault("acc_wcagLevel", "WCAG 2.1 Level AA");
            var keyPages = req.GetValueOrDefault("acc_keyPages");
            var knownIssues = req.GetValueOrDefault("acc_knownIssues");

            sb.AppendLine($"- **Target Compliance Standard**: {wcagLevel}");
            if (!string.IsNullOrWhiteSpace(keyPages)) sb.AppendLine($"- **Pages / Components to Audit**: {keyPages}");
            if (!string.IsNullOrWhiteSpace(knownIssues)) sb.AppendLine($"- **Known Issues to Verify / Track**: {knownIssues}");
            sb.AppendLine();
            sb.AppendLine("**Accessibility checklist:**");
            sb.AppendLine("1. All images have descriptive `alt` text, or `alt=\"\"` for purely decorative images.");
            sb.AppendLine("2. All form inputs are associated with visible labels via `for`/`id` or `aria-label`.");
            sb.AppendLine("3. Color contrast ratios meet the target level (AA: 4.5:1 normal text, 3:1 large text).");
            sb.AppendLine("4. All interactive elements are fully keyboard-navigable with visible focus indicators.");
            sb.AppendLine("5. ARIA roles and attributes are used correctly and do not conflict with native HTML semantics.");
            sb.AppendLine("6. No information is conveyed by color alone — always paired with text, icon, or pattern.");
            sb.AppendLine("7. Error messages are programmatically associated with their input via `aria-describedby`.");
            sb.AppendLine("8. Heading hierarchy is logical: h1 → h2 → h3, no levels skipped.");
            sb.AppendLine("9. Dynamic content changes (modals, toasts, alerts) are announced to screen readers via ARIA live regions.");
            sb.AppendLine();
        }
    }
}
