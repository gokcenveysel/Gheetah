namespace Gheetah.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(string userId, string action, string details);
    }
}
