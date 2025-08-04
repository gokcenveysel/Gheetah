namespace Gheetah.Models.Hangfire
{
    public class JobInfoDto
    {
        public string Id { get; set; }
        public string State { get; set; }
        public DateTime CreatedAt { get; set; }
        public string MethodName { get; set; }
        public string Arguments { get; set; }
        public string FormattedCreatedAt => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
    }

}
