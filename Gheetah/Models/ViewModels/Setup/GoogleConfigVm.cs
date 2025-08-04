namespace Gheetah.Models.ViewModels.Setup
{
    public class GoogleConfigVm
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string CallbackPath { get; set; }
        public string Authority => "https://accounts.google.com";

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(ClientId) &&
                   !string.IsNullOrEmpty(ClientSecret);
        }
    }

}
