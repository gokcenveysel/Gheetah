using Gheetah.Models.AiModels;
using System.Text.Json;

namespace Gheetah.Services
{
    public class GheetahAiAgentProtocol
    {
        public GaapMessage BuildInitMessage(AiScenario scenario, EnvironmentConfig env, string prePrompt, string sessionId)
        {
            var payload = new
            {
                sessionId,
                projectId = scenario.ProjectId,
                scenarioId = scenario.Id,
                prePrompt,
                environment = new
                {
                    baseUrl = env?.BaseUrl,
                    browserType = env?.BrowserType,
                    variables = env?.Variables
                }
            };

            return new GaapMessage
            {
                MessageType = GaapMessageType.Initialize,
                SessionId = sessionId,
                Payload = JsonSerializer.Serialize(payload)
            };
        }

        public GaapMessage BuildScenarioRequest(AiScenario scenario, string sessionId)
        {
            var payload = new
            {
                sessionId,
                scenarioId = scenario.Id,
                featureName = scenario.FeatureName,
                title = scenario.Title,
                gherkinContent = scenario.GherkinContent,
                tags = scenario.Tags
            };

            return new GaapMessage
            {
                MessageType = GaapMessageType.ScenarioRequest,
                SessionId = sessionId,
                Payload = JsonSerializer.Serialize(payload)
            };
        }

        public AiExecutionResult ParseCompletion(string sessionId, List<GaapMessage> transcript)
        {
            var stepResults = new List<AiStepResult>();
            var outputChunks = new List<string>();
            string errorMessage = null;
            var status = AiExecutionStatus.Passed;

            foreach (var msg in transcript)
            {
                switch (msg.MessageType)
                {
                    case GaapMessageType.OutputChunk:
                        outputChunks.Add(msg.Payload);
                        break;
                    case GaapMessageType.StepCompleted:
                        var step = DeserializeSafe<AiStepResult>(msg.Payload);
                        if (step != null) stepResults.Add(step);
                        break;
                    case GaapMessageType.StepFailed:
                        var failedStep = DeserializeSafe<AiStepResult>(msg.Payload);
                        if (failedStep != null)
                        {
                            failedStep.Passed = false;
                            stepResults.Add(failedStep);
                            status = AiExecutionStatus.Failed;
                        }
                        break;
                    case GaapMessageType.Error:
                        errorMessage = msg.Payload;
                        status = AiExecutionStatus.AgentError;
                        break;
                }
            }

            return new AiExecutionResult
            {
                SessionId = sessionId,
                Status = status,
                StepResults = stepResults,
                OutputChunks = outputChunks,
                ErrorMessage = errorMessage
            };
        }

        public bool IsHeartbeat(GaapMessage msg) => msg.MessageType == GaapMessageType.Heartbeat;
        public bool IsError(GaapMessage msg) => msg.MessageType == GaapMessageType.Error;
        public bool IsComplete(GaapMessage msg) => msg.MessageType == GaapMessageType.ScenarioComplete;
        public bool IsAbort(GaapMessage msg) => msg.MessageType == GaapMessageType.Abort;

        private static T DeserializeSafe<T>(string json) where T : class
        {
            try { return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); }
            catch { return null; }
        }
    }
}
