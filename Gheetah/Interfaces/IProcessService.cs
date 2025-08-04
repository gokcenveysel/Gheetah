using Gheetah.Models.ProcessModel;
using System.Threading.Tasks;

namespace Gheetah.Interfaces
{
    public interface IProcessService
    {
        Task ExecuteProcessAsync(string command, ProcessInfo processInfo, string testResultsFilePath);
        string StartProcess(string userId, Func<ProcessInfo, CancellationToken, Task> processAction);
        ProcessInfo GetProcess(string processId);
        List<ProcessInfo> GetUserProcesses(string userId);
        void UpdateProcess(string processId, Action<ProcessInfo> updateAction);
        IEnumerable<ProcessInfo> GetRecentProcesses(string userId, int count);
        bool CancelProcess(string processId, string userId);

        bool AddProcess(ProcessInfo processInfo);
    }
}
