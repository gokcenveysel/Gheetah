using Gheetah.Models.ProcessModel;

namespace Gheetah.Interfaces
{
    public interface ITestResultProcessor
    {
        Task ProcessTestResultsAsync(ProcessInfo processInfo, string testResultsFilePath);
    }
}
