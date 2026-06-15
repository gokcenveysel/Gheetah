using Gheetah.Models.AiModels;

namespace Gheetah.Interfaces
{
    public interface IAiAgentAdapter
    {
        string ProviderType { get; }
        Task<bool> IsHealthyAsync(AiAgent agent);
        Task<string> GenerateAsync(AiAgent agent, string prompt, CancellationToken ct = default);
        Task<bool> InitializeSessionAsync(AiAgent agent, GaapMessage initMessage);
        IAsyncEnumerable<GaapMessage> StreamOutputAsync(AiAgent agent, GaapMessage request, CancellationToken ct);
        Task TerminateSessionAsync(AiAgent agent, string sessionId);
    }
}
