using Gheetah.Interfaces;

namespace Gheetah.Services.AiAdapters
{
    public class AiAgentAdapterFactory
    {
        private readonly IServiceProvider _sp;

        public AiAgentAdapterFactory(IServiceProvider sp)
        {
            _sp = sp;
        }

        public IAiAgentAdapter Create(string providerType) => providerType switch
        {
            "Claude" => _sp.GetRequiredService<ClaudeComputerUseAdapter>(),
            "OpenAI" => _sp.GetRequiredService<OpenAIOperatorAdapter>(),
            "Gemini" => _sp.GetRequiredService<GeminiAgentAdapter>(),
            "Grok" => _sp.GetRequiredService<GrokAgentAdapter>(),
            "MCP" => _sp.GetRequiredService<McpServerAdapter>(),
            "Mock" => _sp.GetRequiredService<MockAgentAdapter>(),
            _ => _sp.GetRequiredService<CustomAgentAdapter>()
        };
    }
}
