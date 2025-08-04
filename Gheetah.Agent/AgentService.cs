using Gheetah.Agent.Model;
using Microsoft.AspNetCore.SignalR.Client;
using System.IO.Compression;
using System.Text.Json;

namespace Gheetah.Agent
{
    public class AgentService
    {
        private static HubConnection? _connection;
        private static int _runningScenarios = 0;
        private static readonly int MaxParallelScenarios = 4;
        private static string _agentId;
        private static readonly object _lock = new object();
        private static readonly string _configFilePath = Path.Combine(Environment.CurrentDirectory, "Data", "agent-config.json");
        private static readonly string _serverInfoPath = Path.Combine(Environment.CurrentDirectory, "Data", "server-info.json");

        static AgentService()
        {
            var config = LoadAgentConfig();
            StatusUI.ShowStatus($"Static constructor: Loaded agent config: {JsonSerializer.Serialize(config)}"); // Debug log
            if (config.IsDeclined)
            {
                StatusUI.ShowStatus("Your first request declined by Admin, please contact with Admin and ask a permission");
                StatusUI.ShowStatus("Exiting due to declined status.");
            }
            if (string.IsNullOrEmpty(config.AgentId))
            {
                _agentId = Guid.NewGuid().ToString();
                config.AgentId = _agentId;
                SaveAgentConfig(config);
            }
            else
            {
                _agentId = config.AgentId;
            }
        }

        public static async Task InitializeAsync()
        {
            var config = LoadAgentConfig();
            StatusUI.ShowStatus($"InitializeAsync: Loaded agent config: {JsonSerializer.Serialize(config)}");
            if (config.IsDeclined)
            {
                StatusUI.ShowStatus("Your first request declined by Admin, please contact with Admin and ask a permission");
                return;
            }

            var serverInfo = await LoadServerInfoAsync();
            if (serverInfo == null)
            {
                StatusUI.ShowStatus("Enter Gheetah Server URL (ex: https://gheetah-server.com): ");
                string serverUrl = Console.ReadLine();
    
                if (string.IsNullOrEmpty(serverUrl))
                {
                    StatusUI.ShowStatus("Invalid server URL!");
                    return;
                }

                // Trim and remove trailing slash
                serverUrl = serverUrl.Trim();
                if (serverUrl.EndsWith("/"))
                {
                    serverUrl = serverUrl.Substring(0, serverUrl.Length - 1);
                }

                serverInfo = new ServerInfo { ServerUrl = serverUrl };
    
                try
                {
                    var json = JsonSerializer.Serialize(serverInfo, new JsonSerializerOptions { WriteIndented = true });
                    Directory.CreateDirectory(Path.GetDirectoryName(_serverInfoPath));
                    await File.WriteAllTextAsync(_serverInfoPath, json);
                    StatusUI.ShowStatus("Server info saved.");
                }
                catch (Exception ex)
                {
                    StatusUI.ShowStatus($"Server info save error: {ex.Message}");
                    return;
                }
            }

            if (_connection == null)
            {
                await RegisterToServerAsync(serverInfo, config.IsRegistered, config.IsDeclined);
            }

            if (_connection == null) return;

            _connection.On<string>("ReceiveStatus", async message =>
            {
                StatusUI.ShowStatus($"Received status message: {message}");
                var currentConfig = LoadAgentConfig();
                bool configChanged = false;

                if (message == "Agent registered successfully")
                {
                    if (!currentConfig.IsRegistered || currentConfig.ConnectionId != _connection.ConnectionId)
                    {
                        currentConfig.IsRegistered = true;
                        currentConfig.ConnectionId = _connection.ConnectionId;
                        currentConfig.IsDeclined = false;
                        configChanged = true;
                    }
                    StatusUI.ShowStatus("Registration is completed! Admin accept your request, you can send a test your agent right now");
                }
                else if (message == "Your register request declined by Admin, please contact with Admin")
                {
                    StatusUI.ShowStatus("Processing decline message...");
                    currentConfig.IsRegistered = false;
                    currentConfig.AgentId = null;
                    currentConfig.ConnectionId = null;
                    currentConfig.IsDeclined = true;
                    configChanged = true;
                    if (File.Exists(_serverInfoPath))
                    {
                        try
                        {
                            File.Delete(_serverInfoPath);
                            StatusUI.ShowStatus("Server info deleted due to admin decline.");
                        }
                        catch (Exception ex)
                        {
                            StatusUI.ShowStatus($"Failed to delete server info: {ex.Message}");
                        }
                    }
                    SaveAgentConfig(currentConfig);
                    StatusUI.ShowStatus("Your register request declined by Admin, please contact with Admin");
                    await StopAsync();
                    StatusUI.ShowStatus("Agent connection closed.");
                    StatusUI.ShowStatus("Exiting due to admin decline.");
                }
                else if (message == "Agent deleted by Admin")
                {
                    currentConfig.IsRegistered = false;
                    currentConfig.AgentId = null;
                    currentConfig.ConnectionId = null;
                    currentConfig.IsDeclined = false;
                    configChanged = true;
                    if (File.Exists(_serverInfoPath))
                    {
                        try
                        {
                            File.Delete(_serverInfoPath);
                            StatusUI.ShowStatus("Server info deleted due to admin deletion.");
                        }
                        catch (Exception ex)
                        {
                            StatusUI.ShowStatus($"Failed to delete server info: {ex.Message}");
                        }
                    }
                    StatusUI.ShowStatus("Agent deleted by Admin.");
                    await StopAsync();
                    StatusUI.ShowStatus("Agent connection closed.");
                }
                else if (message == "Agent is already registered or pending")
                {
                    if (!currentConfig.IsRegistered || currentConfig.ConnectionId != _connection.ConnectionId)
                    {
                        currentConfig.IsRegistered = true;
                        currentConfig.ConnectionId = _connection.ConnectionId;
                        currentConfig.IsDeclined = false;
                        configChanged = true;
                    }
                    StatusUI.ShowStatus("Agent is already registered or pending approval. Connecting...");
                }
                else if (message == "Your first request declined by Admin, please contact with Admin and ask a permission")
                {
                    currentConfig.IsRegistered = false;
                    currentConfig.AgentId = null;
                    currentConfig.ConnectionId = null;
                    currentConfig.IsDeclined = true;
                    configChanged = true;
                    if (File.Exists(_serverInfoPath))
                    {
                        try
                        {
                            File.Delete(_serverInfoPath);
                            StatusUI.ShowStatus("Server info deleted due to admin decline.");
                        }
                        catch (Exception ex)
                        {
                            StatusUI.ShowStatus($"Failed to delete server info: {ex.Message}");
                        }
                    }
                    StatusUI.ShowStatus("Your first request declined by Admin, please contact with Admin and ask a permission");
                    await StopAsync();
                    StatusUI.ShowStatus("Agent connection closed.");
                }

                if (configChanged)
                {
                    SaveAgentConfig(currentConfig);
                }
            });

            _connection.On("Ping", async () =>
            {
                if (!config.IsDeclined)
                {
                    await _connection.InvokeAsync("Pong", _agentId, _connection.ConnectionId, _runningScenarios);
                    await UpdateAvailability("Ping");
                }
            });

            _connection.On<string, string, string, string>("ExecuteScenario", async (processId, scenarioTag, languageType, buildedTestFileName) =>
            {
                StatusUI.ShowStatus($"ExecuteScenario called: ProcessId={processId}, ScenarioTag={scenarioTag}, LanguageType={languageType}, BuildedTestFileName={buildedTestFileName}");

                StatusUI.ShowStatus($"Checking parallel scenarios: Current={_runningScenarios}, Max={MaxParallelScenarios}");
                if (_runningScenarios >= MaxParallelScenarios)
                {
                    StatusUI.ShowStatus($"Max parallel scenarios reached: {_runningScenarios}");
                    await SendOutputAsync($"Error: Max parallel scenarios reached: {_runningScenarios}", processId);
                    await SendResultAsync($"Error: Max parallel scenarios reached: {_runningScenarios}", processId);
                    return;
                }

                lock (_lock)
                {
                    _runningScenarios++;
                    StatusUI.ShowStatus($"Incremented _runningScenarios: {_runningScenarios}");
                }

                StatusUI.ShowStatus("ExecuteScenario Start");
                try
                {
                    await UpdateAvailability("ExecuteScenario Start");
                }
                catch (Exception ex)
                {
                    StatusUI.ShowStatus($"UpdateAvailability failed in ExecuteScenario: {ex.Message}, continuing execution...");
                }

                try
                {
                    string extractPath = Path.Combine(Path.GetTempPath(), $"Gheetah_{processId}");
                    StatusUI.ShowStatus($"Checking extract path: {extractPath}");
                    if (!Directory.Exists(extractPath))
                    {
                        StatusUI.ShowStatus($"Error: Extracted project directory not found: {extractPath}");
                        await SendOutputAsync($"Error: Extracted project directory not found: {extractPath}", processId);
                        await SendResultAsync($"Error: Extracted project directory not found: {extractPath}", processId);
                        return;
                    }

                    string[] files = Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories);
                    StatusUI.ShowStatus($"Extracted directory contents: {string.Join(", ", files)}");

                    StatusUI.ShowStatus($"Executing scenario in directory: {extractPath}");
                    if (languageType.Equals("C#", StringComparison.OrdinalIgnoreCase))
                    {
                        StatusUI.ShowStatus("Calling CSharpScenarioExecutor.ExecuteAsync");
                        await CSharpScenarioExecutor.ExecuteAsync(extractPath, scenarioTag, processId, buildedTestFileName);
                    }
                    else if (languageType.Equals("java", StringComparison.OrdinalIgnoreCase))
                    {
                        StatusUI.ShowStatus("Calling JavaScenarioExecutor.ExecuteAsync");
                        await JavaScenarioExecutor.ExecuteAsync(extractPath, scenarioTag, processId);
                    }
                    else
                    {
                        StatusUI.ShowStatus($"Unsupported language type: {languageType}");
                        await SendOutputAsync($"Unsupported language type: {languageType}", processId);
                        await SendResultAsync($"Unsupported language type: {languageType}", processId);
                    }
                    // Completion mesajı gönder
                    await SendResultAsync($"Scenario execution completed for processId: {processId}", processId);
                }
                catch (Exception ex)
                {
                    StatusUI.ShowStatus($"Error executing scenario: {ex.Message}, StackTrace: {ex.StackTrace}");
                    await SendOutputAsync($"Error executing scenario: {ex.Message}", processId);
                    await SendResultAsync($"Error: Scenario execution failed: {ex.Message}", processId);
                }
                finally
                {
                    lock (_lock)
                    {
                        _runningScenarios--;
                        StatusUI.ShowStatus($"Decremented _runningScenarios: {_runningScenarios}");
                    }
                    StatusUI.ShowStatus("ExecuteScenario End");
                    try
                    {
                        await UpdateAvailability("ExecuteScenario End");
                    }
                    catch (Exception ex)
                    {
                        StatusUI.ShowStatus($"UpdateAvailability failed in ExecuteScenario finally: {ex.Message}, continuing...");
                    }
                }
            });

            _connection.On<string, string, string>("ExecuteAllScenarios", async (processId, languageType, buildedTestFileName) =>
            {
                StatusUI.ShowStatus($"ExecuteAllScenarios called: ProcessId={processId}, LanguageType={languageType}");
                if (_runningScenarios >= MaxParallelScenarios)
                {
                    StatusUI.ShowStatus($"Max parallel scenarios reached: {_runningScenarios}");
                    await SendOutputAsync($"Error: Max parallel scenarios reached: {_runningScenarios}", processId);
                    await SendResultAsync($"Error: Max parallel scenarios reached: {_runningScenarios}", processId);
                    return;
                }
                lock (_lock)
                {
                    _runningScenarios++;
                }
                await UpdateAvailability("ExecuteAllScenarios Start");

                try
                {
                    string extractPath = Path.Combine(Path.GetTempPath(), $"Gheetah_{processId}");
                    if (!Directory.Exists(extractPath))
                    {
                        await SendOutputAsync($"Error: Extracted project directory not found: {extractPath}", processId);
                        await SendResultAsync($"Error: Extracted project directory not found: {extractPath}", processId);
                        return;
                    }

                    StatusUI.ShowStatus($"Executing all scenarios in directory: {extractPath}");
                    if (languageType.Equals("C#", StringComparison.OrdinalIgnoreCase))
                    {
                        StatusUI.ShowStatus("Calling CSharpAllScenariosExecutor.ExecuteAllAsync");
                        await CSharpAllScenariosExecutor.ExecuteAllAsync(extractPath, processId, buildedTestFileName);
                    }
                    else if (languageType.Equals("java", StringComparison.OrdinalIgnoreCase))
                    {
                        StatusUI.ShowStatus("Calling JavaAllScenariosExecutor.ExecuteAllAsync");
                        await JavaAllScenariosExecutor.ExecuteAllAsync(extractPath, processId);
                    }
                    else
                    {
                        await SendOutputAsync($"Unsupported language type: {languageType}", processId);
                        await SendResultAsync($"Unsupported language type: {languageType}", processId);
                    }

                    await SendResultAsync($"All scenarios execution completed for processId: {processId}", processId);
                }
                catch (Exception ex)
                {
                    StatusUI.ShowStatus($"Error executing all scenarios: {ex.Message}");
                    await SendOutputAsync($"Error executing all scenarios: {ex.Message}", processId);
                    await SendResultAsync($"Error: All scenarios execution failed: {ex.Message}", processId);
                }
                finally
                {
                    lock (_lock)
                    {
                        _runningScenarios--;
                    }
                    await UpdateAvailability("ExecuteAllScenarios End");
                }
            });

            _connection.On<string, string, byte[]>("SendZipFile", async (agentId, processId, zipData) =>
            {
                StatusUI.ShowStatus($"SendZipFile called: AgentId={agentId}, ProcessId={processId}, ZipSize={zipData.Length} bytes");
                try
                {
                    string zipPath = Path.Combine(Path.GetTempPath(), $"Gheetah_{processId}.zip");
                    Directory.CreateDirectory(Path.GetDirectoryName(zipPath));
                    await File.WriteAllBytesAsync(zipPath, zipData);
                    StatusUI.ShowStatus($"Zip file saved: {zipPath}, Size: {zipData.Length} bytes");

                    string extractPath = Path.Combine(Path.GetTempPath(), $"Gheetah_{processId}");
                    Directory.CreateDirectory(extractPath);
                    ZipFile.ExtractToDirectory(zipPath, extractPath, true);
                    StatusUI.ShowStatus($"Zip extracted to: {extractPath}");

                    await SendOutputAsync($"Zip file received and extracted for process {processId}.", processId);
                }
                catch (Exception ex)
                {
                    StatusUI.ShowStatus($"Zip file receive/extract error: {ex.Message}");
                    await SendOutputAsync($"Error receiving/extracting zip: {ex.Message}", processId);
                    await SendResultAsync($"Error: Zip file processing failed: {ex.Message}", processId);
                }
            });
        }

        public static async Task RegisterToServerAsync(ServerInfo serverInfo, bool isRegistered, bool isDeclined)
        {
            StatusUI.ShowStatus($"RegisterToServerAsync: isRegistered={isRegistered}, isDeclined={isDeclined}");
            if (isDeclined)
            {
                StatusUI.ShowStatus("Your first request declined by Admin, please contact with Admin and ask a permission");
                return;
            }

            if (serverInfo == null || string.IsNullOrEmpty(serverInfo.ServerUrl))
            {
                StatusUI.ShowStatus("Invalid server information!");
                return;
            }

            _connection = new HubConnectionBuilder()
                .WithUrl($"{serverInfo.ServerUrl}/gheetahHub")
                .WithAutomaticReconnect()
                .Build();

            _connection.Closed += async (error) =>
            {
                StatusUI.ShowStatus($"Connection closed: {error?.Message}");
                await Task.Delay(new Random().Next(0, 5) * 1000);
                try
                {
                    await _connection.StartAsync();
                    StatusUI.ShowStatus($"Reconnected: ConnectionId: {_connection.ConnectionId}");
                    var config = LoadAgentConfig();
                    if (config.IsDeclined)
                    {
                        StatusUI.ShowStatus("Your first request declined by Admin, please contact with Admin and ask a permission");
                        await StopAsync();
                        return;
                    }
                    if (config.ConnectionId != _connection.ConnectionId)
                    {
                        config.ConnectionId = _connection.ConnectionId;
                        SaveAgentConfig(config);
                    }
                    await _connection.InvokeAsync("Pong", _agentId, _connection.ConnectionId, _runningScenarios);
                }
                catch (Exception ex)
                {
                    StatusUI.ShowStatus($"Reconnection error: {ex.Message}");
                }
            };

            await _connection.StartAsync();
            StatusUI.ShowStatus($"Connection started: ConnectionId = {_connection.ConnectionId}");
            var config = LoadAgentConfig();
            if (config.IsDeclined)
            {
                StatusUI.ShowStatus("Your first request declined by Admin, please contact with Admin and ask a permission");
                await StopAsync();
                return;
            }
            if (config.ConnectionId != _connection.ConnectionId)
            {
                config.ConnectionId = _connection.ConnectionId;
                SaveAgentConfig(config);
            }

            if (!isRegistered)
            {
                var agentInfo = new AgentInfo
                {
                    AgentId = _agentId,
                    ConnectionId = _connection.ConnectionId,
                    OS = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                    EnvironmentName = Environment.MachineName,
                    Status = "pending",
                    Availability = "not available"
                };
                await _connection.InvokeAsync("RegisterAgent", agentInfo);
                StatusUI.ShowStatus("Register request sent to Server");
            }
            else
            {
                await _connection.InvokeAsync("Pong", _agentId, _connection.ConnectionId, _runningScenarios);
                StatusUI.ShowStatus("Agent is already registered. Reconnecting to server...");
            }
        }

        public static async Task SendOutputAsync(string output, string processId)
        {
            if (_connection?.State != HubConnectionState.Connected)
            {
                StatusUI.ShowStatus($"Connection not active (state: {_connection?.State}), attempting to reconnect...");
                try
                {
                    await _connection.StartAsync();
                    StatusUI.ShowStatus($"Reconnected: ConnectionId: {_connection.ConnectionId}");
                }
                catch (Exception ex)
                {
                    StatusUI.ShowStatus($"Reconnection failed: {ex.Message}");
                    return;
                }
            }
            await _connection.InvokeAsync("ReceiveOutput", output, processId);
            StatusUI.ShowStatus($"Output sent: {output}");
        }

        public static async Task SendResultAsync(string result, string processId)
        {
            if (_connection?.State != HubConnectionState.Connected)
            {
                StatusUI.ShowStatus($"Connection not active (state: {_connection?.State}), attempting to reconnect...");
                try
                {
                    await _connection.StartAsync();
                    StatusUI.ShowStatus($"Reconnected: ConnectionId: {_connection.ConnectionId}");
                }
                catch (Exception ex)
                {
                    StatusUI.ShowStatus($"Reconnection failed: {ex.Message}");
                    return;
                }
            }
            await _connection.InvokeAsync("ReceiveResult", result, processId);
            StatusUI.ShowStatus($"Result sent: {result}");
        }

        public static async Task UpdateAvailability(string status)
        {
            StatusUI.ShowStatus($"UpdateAvailability called: Status={status}, ConnectionState={_connection?.State}");
            if (_connection?.State == HubConnectionState.Connected)
            {
                try
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        await _connection.InvokeAsync("UpdateAvailability", _agentId, _runningScenarios < MaxParallelScenarios ? "available" : "busy", cts.Token);
                        StatusUI.ShowStatus($"Availability updated: {status}, RunningScenarios: {_runningScenarios}, AgentId: {_agentId}");
                    }
                }
                catch (OperationCanceledException)
                {
                    StatusUI.ShowStatus($"UpdateAvailability timed out: Status={status}");
                }
                catch (Exception ex)
                {
                    StatusUI.ShowStatus($"UpdateAvailability error: Status={status}, Error={ex.Message}, StackTrace={ex.StackTrace}");
                }
            }
            else
            {
                StatusUI.ShowStatus($"UpdateAvailability skipped: Connection is not in Connected state, CurrentState={_connection?.State}");
            }
        }

        public static async Task StopAsync()
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                await _connection.StopAsync();
                StatusUI.ShowStatus("Agent connection closed.");
            }
        }

        private static async Task<ServerInfo> LoadServerInfoAsync()
        {
            try
            {
                if (File.Exists(_serverInfoPath))
                {
                    var json = await File.ReadAllTextAsync(_serverInfoPath);
                    StatusUI.ShowStatus($"Loaded server info: {json}");
                    return JsonSerializer.Deserialize<ServerInfo>(json);
                }
                return null;
            }
            catch (Exception ex)
            {
                StatusUI.ShowStatus($"Server info load error: {ex.Message}");
                return null;
            }
        }

        private static AgentConfig LoadAgentConfig()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    var config = JsonSerializer.Deserialize<AgentConfig>(json);
                    if (config == null)
                    {
                        StatusUI.ShowStatus("Agent config deserialization failed, returning new config.");
                        return new AgentConfig();
                    }
                    StatusUI.ShowStatus($"Loaded agent config from file: {json}");
                    return config;
                }
                StatusUI.ShowStatus("Agent config file not found, returning new config.");
                return new AgentConfig();
            }
            catch (Exception ex)
            {
                StatusUI.ShowStatus($"Agent config load error: {ex.Message}");
                throw new Exception("Failed to load agent config.", ex);
            }
        }

        private static void SaveAgentConfig(AgentConfig config)
        {
            try
            {
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                StatusUI.ShowStatus($"Saving agent config: {json}");
                Directory.CreateDirectory(Path.GetDirectoryName(_configFilePath));
                File.WriteAllText(_configFilePath, json);
                StatusUI.ShowStatus("Agent config saved.");
            }
            catch (Exception ex)
            {
                StatusUI.ShowStatus($"Agent config save error: {ex.Message}");
                throw new Exception("Failed to save agent config.", ex);
            }
        }

        private static async Task<string> ReceiveZipFileAsync(string processId)
        {
            string zipPath = Path.Combine(Path.GetTempPath(), $"Gheetah_{processId}.zip");
            if (File.Exists(zipPath))
            {
                return zipPath;
            }
            return null;
        }

        public static string GetTestResultsFilePath(string buildedTestFileFullPath, string scenarioTag = null)
        {
            var testResultsFolder = Path.Combine(buildedTestFileFullPath, "TestResults");
            if (!Directory.Exists(testResultsFolder))
            {
                Directory.CreateDirectory(testResultsFolder);
            }
            if (string.IsNullOrEmpty(scenarioTag))
            {
                return Path.Combine(testResultsFolder, $"{DateTime.Now:yyyyMMdd_HHmmss}_test_results.xml");
            }
            else
            {
                return Path.Combine(testResultsFolder, $"{scenarioTag}_{DateTime.Now:yyyyMMdd_HHmmss}_test_results.xml");
            }
        }
    }
}