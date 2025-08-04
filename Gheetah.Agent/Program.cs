namespace Gheetah.Agent
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Gheetah Agent Starting...");
            await AgentService.InitializeAsync();
            Console.WriteLine("Agent is working. Please combine Ctrl+C for exit.");
            await Task.Delay(Timeout.Infinite);
        }
    }
}