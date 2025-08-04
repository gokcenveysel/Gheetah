namespace Gheetah.Models.Hangfire
{
    public class JobDto
    {
        public string Id { get; set; }
        public string State { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string MethodName { get; set; }
    }
}
