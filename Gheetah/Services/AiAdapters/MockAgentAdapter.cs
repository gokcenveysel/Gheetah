using Gheetah.Interfaces;
using Gheetah.Models.AiModels;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Gheetah.Services.AiAdapters
{
    // Mock adapter for UI testing — no real API calls, returns deterministic fake output.
    public class MockAgentAdapter : IAiAgentAdapter
    {
        public string ProviderType => "Mock";

        public Task<bool> IsHealthyAsync(AiAgent agent) => Task.FromResult(true);

        public Task<bool> InitializeSessionAsync(AiAgent agent, GaapMessage initMessage) => Task.FromResult(true);

        public Task TerminateSessionAsync(AiAgent agent, string sessionId) => Task.CompletedTask;

        public async Task<string> GenerateAsync(AiAgent agent, string prompt, CancellationToken ct = default)
        {
            await Task.Delay(800, ct); // simulate network latency

            var topic = ExtractTopic(prompt);
            var featureName = DeriveFeatureName(topic);
            var scenarioTitle = DeriveScenarioTitle(topic);

            return $@"Feature: {featureName}
  As a user
  I want to {topic.ToLowerInvariant().TrimEnd('.')}
  So that I can complete my goal successfully

  Scenario: {scenarioTitle}
    Given the user is on the application
    And the user is authenticated
    When the user performs the action for ""{topic}""
    And the system processes the request
    Then the operation completes successfully
    And the user sees a confirmation message
    And the system state is updated correctly

  Scenario: {scenarioTitle} - Failure case
    Given the user is on the application
    And the user is authenticated
    When the user provides invalid input for ""{topic}""
    Then the system displays an appropriate error message
    And the operation is not completed
    And the system state remains unchanged";
        }

        public async IAsyncEnumerable<GaapMessage> StreamOutputAsync(
            AiAgent agent,
            GaapMessage request,
            [EnumeratorCancellation] CancellationToken ct)
        {
            var sessionId = request.SessionId ?? Guid.NewGuid().ToString();

            var steps = new[]
            {
                (GaapMessageType.StepStarted,   "Given the user is on the application"),
                (GaapMessageType.StepCompleted, "Given the user is on the application"),
                (GaapMessageType.StepStarted,   "And the user is authenticated"),
                (GaapMessageType.StepCompleted, "And the user is authenticated"),
                (GaapMessageType.StepStarted,   "When the user performs the action"),
                (GaapMessageType.StepCompleted, "When the user performs the action"),
                (GaapMessageType.StepStarted,   "Then the operation completes successfully"),
                (GaapMessageType.StepCompleted, "Then the operation completes successfully"),
            };

            yield return Msg(GaapMessageType.OutputChunk, sessionId, "[Mock Agent] Starting scenario execution...");
            await Task.Delay(300, ct);

            foreach (var (type, text) in steps)
            {
                if (ct.IsCancellationRequested) yield break;
                yield return Msg(type, sessionId, text);
                await Task.Delay(type == GaapMessageType.StepStarted ? 200 : 400, ct);
            }

            yield return Msg(GaapMessageType.OutputChunk, sessionId, "[Mock Agent] All steps passed.");
            await Task.Delay(150, ct);
            yield return Msg(GaapMessageType.ScenarioComplete, sessionId, "{\"status\":\"Passed\",\"durationMs\":1500}");
        }

        private static GaapMessage Msg(GaapMessageType type, string sessionId, string payload) => new()
        {
            MessageType = type,
            SessionId = sessionId,
            Payload = payload,
            Timestamp = DateTime.UtcNow
        };

        private static string ExtractTopic(string prompt)
        {
            var match = Regex.Match(prompt, @"for:\s*\n(.+?)(?:\n|$)", RegexOptions.Multiline);
            return match.Success ? match.Groups[1].Value.Trim() : "the requested action";
        }

        private static string DeriveFeatureName(string topic)
        {
            var words = topic.Split(' ').Take(5);
            return string.Join(" ", words.Select(w => char.ToUpper(w[0]) + w.Substring(1).ToLower()));
        }

        private static string DeriveScenarioTitle(string topic)
        {
            var t = topic.Trim();
            return char.ToUpper(t[0]) + (t.Length > 1 ? t.Substring(1) : "");
        }
    }
}
