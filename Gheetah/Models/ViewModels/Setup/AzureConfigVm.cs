namespace Gheetah.Models.ViewModels.Setup
{
    public class AzureConfigVm
    {
        public string Instance { get; set; } = "https://login.microsoftonline.com/";
        public string Domain { get; set; }
        public string TenantId { get; set; }
        public string ClientId { get; set; }
        public string CallbackPath { get; set; }
        public string ClientSecret { get; set; }
        public string AzureDevOpsUrl { get; set; }

        public string Authority => $"{Instance}{TenantId}";

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(ClientId) &&
                   !string.IsNullOrEmpty(ClientSecret) &&
                   !string.IsNullOrEmpty(Instance) &&
                   !string.IsNullOrEmpty(TenantId);
        }
    }

}
