using Gheetah.Agent.Model;
using Spectre.Console;
using System.Text.Json;

namespace Gheetah.Agent;

public class RegisterUI
{
    public static async Task RegisterAgentAsync()
    {
        if (File.Exists("Data/server-info.json"))
        {
            StatusUI.ShowStatus("Agent is already registered. Existing server information is being used.");
            return;
        }

        var serverUrl = AnsiConsole.Ask<string>("Enter Gheetah Server URL (ex: https://gheetah-server.com):");

        var serverInfo = new ServerInfo
        {
            ServerUrl = serverUrl
        };

        Directory.CreateDirectory("Data");
        await File.WriteAllTextAsync("Data/server-info.json", JsonSerializer.Serialize(serverInfo, new JsonSerializerOptions { WriteIndented = true }));
        StatusUI.ShowStatus("Server info saved.");
    }
}