using Gheetah.Hub;
using Gheetah.Interfaces;
using Gheetah.Models.ProcessModel;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.RegularExpressions;

public class ProcessService : IProcessService
{
    private readonly ConcurrentDictionary<string, ProcessInfo> _processes = new();
    private readonly IHubContext<GheetahHub> _hubContext;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ITestResultProcessor _testResultProcessor;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ProcessService(
        IHttpContextAccessor httpContextAccessor,
        IHubContext<GheetahHub> hubContext,
        IBackgroundJobClient backgroundJobClient,
        ITestResultProcessor testResultProcessor)
    {
        _httpContextAccessor = httpContextAccessor;
        _hubContext = hubContext;
        _backgroundJobClient = backgroundJobClient;
        _testResultProcessor = testResultProcessor;
        Console.WriteLine($"ProcessService instantiated, instance: {GetHashCode()}, thread: {Thread.CurrentThread.ManagedThreadId}");
    }

    public bool AddProcess(ProcessInfo processInfo)
    {
        Console.WriteLine($"AddProcess called for processId: {processInfo.Id}, userId: {processInfo.UserId}, instance: {GetHashCode()}, thread: {Thread.CurrentThread.ManagedThreadId}");
        bool added = _processes.TryAdd(processInfo.Id, processInfo);
        if (!added)
        {
            Console.WriteLine($"Failed to add processId: {processInfo.Id} to _processes");
        }
        else
        {
            Console.WriteLine($"Successfully added processId: {processInfo.Id} to _processes, current count: {_processes.Count}, keys: {string.Join(", ", _processes.Keys)}");
        }
        return added;
    }

    public ProcessInfo GetProcess(string processId)
    {
        Console.WriteLine($"GetProcess called for processId: {processId}, instance: {GetHashCode()}, thread: {Thread.CurrentThread.ManagedThreadId}, current count: {_processes.Count}, keys: {string.Join(", ", _processes.Keys)}");
        bool found = _processes.TryGetValue(processId, out var process);
        if (!found)
        {
            Console.WriteLine($"Process not found for processId: {processId}");
            return null;
        }
        Console.WriteLine($"Process found for processId: {processId}, status: {process.Status}");
        return process;
    }
    public async Task ExecuteProcessAsync(string command, ProcessInfo processInfo, string testResultsFilePath)
    {
        Console.WriteLine($"ExecuteProcessAsync called for processId: {processInfo.Id}, command: {command}");
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy unrestricted -Command \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = new Process())
        {
            process.StartInfo = processStartInfo;

            process.OutputDataReceived += async (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    var cleaned = StripAnsiCodes(e.Data);
                    if (string.IsNullOrWhiteSpace(cleaned)) return;
                    Console.WriteLine($"Process Output for processId: {processInfo.Id}: {cleaned}");
                    processInfo.Output.Add(cleaned);
                    await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveOutput", cleaned);
                }
            };

            process.ErrorDataReceived += async (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    var cleaned = StripAnsiCodes(e.Data);
                    if (string.IsNullOrWhiteSpace(cleaned)) return;
                    Console.WriteLine($"Process Error for processId: {processInfo.Id}: {cleaned}");
                    // Java/Maven INFO and WARNING messages go to stderr but are not real errors.
                    // Only prefix with "Error:" for lines that are genuine error output.
                    var message = IsNonErrorStderr(cleaned) ? cleaned : $"Error: {cleaned}";
                    processInfo.Output.Add(message);
                    await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveOutput", message);
                }
            };

            process.Start();
            Console.WriteLine($"Process started for processId: {processInfo.Id}");
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();

            if (!string.IsNullOrEmpty(testResultsFilePath) && File.Exists(testResultsFilePath))
            {
                Console.WriteLine($"ProcessServices - Test results file found for processId: {processInfo.Id}: {testResultsFilePath}");
                await _testResultProcessor.ProcessTestResultsAsync(processInfo, testResultsFilePath);
                await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveOutput", $"Test results generated: {testResultsFilePath}");
            }
            else if (!string.IsNullOrEmpty(testResultsFilePath))
            {
                Console.WriteLine($"ProcessServices - Test results file not found for processId: {processInfo.Id}: {testResultsFilePath}");
                await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveOutput", $"Error: Test results file not found at {testResultsFilePath}");
            }

            await _hubContext.Clients.Group(processInfo.Id).SendAsync("ReceiveCompletionMessage", "Test execution completed.");
        }
    }

    private static string StripAnsiCodes(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return Regex.Replace(text, @"\x1B\[[0-9;]*[a-zA-Z]|\x1B[=>]|\x1B\][\s\S]*?(\x07|\x1B\\)", string.Empty);
    }

    /// <summary>
    /// Returns true for stderr lines that are informational (not real errors).
    /// Java/Maven send INFO and WARNING log messages to stderr; these should not be
    /// prefixed with "Error:" since they confuse users when tests actually pass.
    /// </summary>
    private static bool IsNonErrorStderr(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return true;

        // Java logger format:  "[main] INFO  classname - message"
        //                      "[pool-1-thread-1] WARN ..."
        if (Regex.IsMatch(line, @"^\[.+\]\s+(INFO|WARN|WARNING)\s+")) return true;

        // Java timestamp format: "Jan 01, 2026 12:00:00 AM org.xyz.ClassName methodName"
        if (Regex.IsMatch(line, @"^\w{3}\s+\d{1,2},\s+\d{4}\s+\d{1,2}:\d{2}:\d{2}\s+(AM|PM)")) return true;

        // Maven build markers that go to stderr in some environments
        if (line.TrimStart().StartsWith("[INFO]") || line.TrimStart().StartsWith("[WARNING]")) return true;

        // WARNING: lines from Selenium/WebDriverManager CDP mismatches
        if (line.TrimStart().StartsWith("WARNING:") && !line.Contains("FAILURE") && !line.Contains("ERROR")) return true;

        // npm/Node.js informational warnings (e.g., "npm warn ...")
        if (Regex.IsMatch(line, @"^npm\s+(warn|notice|info)\s+", RegexOptions.IgnoreCase)) return true;

        // Playwright informational lines
        if (line.TrimStart().StartsWith("npx: ") || line.TrimStart().StartsWith("Need to install")) return true;

        return false;
    }

    private async Task RemoveProcessAfterDelay(string processId, TimeSpan delay)
    {
        Console.WriteLine($"RemoveProcessAfterDelay scheduled for processId: {processId}, delay: {delay}, instance: {GetHashCode()}, thread: {Thread.CurrentThread.ManagedThreadId}, current count: {_processes.Count}, keys: {string.Join(", ", _processes.Keys)}");
        await Task.Delay(delay);
        var currentUserId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (_processes.TryGetValue(processId, out var process) && (currentUserId == null || process.UserId == currentUserId))
        {
            if (_processes.TryRemove(processId, out _))
            {
                Console.WriteLine($"Process removed from _processes: {processId}, remaining count: {_processes.Count}");
            }
            else
            {
                Console.WriteLine($"Process already removed or not found: {processId}");
            }
        }
        else
        {
            Console.WriteLine($"Process not removed: not found or user mismatch for processId: {processId}, currentUserId: {currentUserId}");
        }
    }

    public string StartProcess(string userId, Func<ProcessInfo, CancellationToken, Task> processAction)
    {
        var processId = Guid.NewGuid().ToString();
        var cancellationTokenSource = new CancellationTokenSource();

        var processInfo = new ProcessInfo
        {
            Id = processId,
            UserId = userId,
            CancellationTokenSource = cancellationTokenSource,
            Status = ProcessStatus.Running,
            StartTime = DateTime.UtcNow
        };

        Console.WriteLine($"StartProcess called for processId: {processId}, userId: {userId}");
        if (!AddProcess(processInfo))
        {
            Console.WriteLine($"Failed to start processId: {processId}");
            throw new Exception("Process creation failed!");
        }

        var jobId = _backgroundJobClient.Enqueue(() => ExecuteProcessWrapperAsync(processId, processAction, cancellationTokenSource.Token));
        processInfo.HangfireJobId = jobId;
        Console.WriteLine($"Hangfire job enqueued for processId: {processId}, jobId: {jobId}");

        return processId;
    }

    public List<ProcessInfo> GetUserProcesses(string userId)
    {
        Console.WriteLine($"GetUserProcesses called for userId: {userId}");
        var processes = _processes.Values.Where(p => p.UserId == userId).ToList();
        Console.WriteLine($"Found {processes.Count} processes for userId: {userId}");
        return processes;
    }

    public void UpdateProcess(string processId, Action<ProcessInfo> updateAction)
    {
        Console.WriteLine($"UpdateProcess called for processId: {processId}");
        if (_processes.TryGetValue(processId, out var process))
        {
            updateAction(process);
            Console.WriteLine($"Process updated for processId: {processId}, status: {process.Status}");
        }
        else
        {
            Console.WriteLine($"Process not found for update, processId: {processId}");
        }
    }

    public IEnumerable<ProcessInfo> GetRecentProcesses(string userId, int count = 10)
    {
        Console.WriteLine($"GetRecentProcesses called for userId: {userId}, count: {count}");
        var processes = _processes.Values
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.StartTime)
            .Take(count);
        Console.WriteLine($"Found {processes.Count()} recent processes for userId: {userId}");
        return processes;
    }

    public bool CancelProcess(string processId, string userId)
    {
        Console.WriteLine($"CancelProcess called for processId: {processId}, userId: {userId}");
        if (_processes.TryGetValue(processId, out var process) && process.UserId == userId)
        {
            if (!string.IsNullOrEmpty(process.HangfireJobId))
            {
                Console.WriteLine($"Deleting Hangfire job for processId: {processId}, jobId: {process.HangfireJobId}");
                BackgroundJob.Delete(process.HangfireJobId);
            }

            process.CancellationTokenSource?.Cancel();
            process.Status = ProcessStatus.Cancelled;
            _hubContext.Clients.Group(processId).SendAsync("ReceiveOutput", "Process cancelled by user");
            Console.WriteLine($"Process cancelled for processId: {processId}");
            return true;
        }
        Console.WriteLine($"Process not found or user mismatch for processId: {processId}, userId: {userId}");
        return false;
    }

    private async Task SendCompletion(string processId)
    {
        if (_processes.TryGetValue(processId, out var process))
        {
            Console.WriteLine($"SendCompletion called for processId: {processId}, status: {process.Status}");
            await _hubContext.Clients.Group(processId).SendAsync("ReceiveCompletionMessage",
                $"Process {processId} completed with status: {process.Status}");
        }
        else
        {
            Console.WriteLine($"Process not found for SendCompletion, processId: {processId}");
        }
    }

    [AutomaticRetry(Attempts = 0)]
    public async Task ExecuteProcessWrapperAsync(
        string processId,
        Func<ProcessInfo, CancellationToken, Task> processAction,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"ExecuteProcessWrapperAsync started for processId: {processId}");
        if (_processes.TryGetValue(processId, out var processInfo))
        {
            try
            {
                await processAction(processInfo, cancellationToken);
                processInfo.Status = ProcessStatus.Executed;
                await SendCompletion(processId);
                Console.WriteLine($"ExecuteProcessWrapperAsync completed for processId: {processId}");
            }
            catch (OperationCanceledException)
            {
                processInfo.Status = ProcessStatus.Cancelled;
                await _hubContext.Clients.Group(processId).SendAsync("ReceiveOutput", "Process cancelled by user");
                Console.WriteLine($"ExecuteProcessWrapperAsync cancelled for processId: {processId}");
            }
            catch (Exception ex)
            {
                processInfo.Status = ProcessStatus.Failed;
                await _hubContext.Clients.Group(processId).SendAsync("ReceiveOutput", $"Process failed: {ex.Message}");
                Console.WriteLine($"ExecuteProcessWrapperAsync failed for processId: {processId}, error: {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"Scheduling RemoveProcessAfterDelay for processId: {processId}");
                _ = RemoveProcessAfterDelay(processId, TimeSpan.FromHours(1));
            }
        }
        else
        {
            Console.WriteLine($"ProcessInfo not found in ExecuteProcessWrapperAsync for processId: {processId}");
        }
    }
}