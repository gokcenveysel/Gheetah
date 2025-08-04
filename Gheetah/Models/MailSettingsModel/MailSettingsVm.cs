namespace Gheetah.Models.MailSettingsModel
{
    public class MailSettingsVm
    {
        public string Id { get; set; }

        public string Provider { get; set; } // "SMTP", "SendGrid", "Azure"

        public string Name { get; set; }

        // Common properties
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastTestDate { get; set; }
        public bool IsActive { get; set; } = true;

        // SMTP Specific Properties
        public string SmtpHost { get; set; }
        public int? SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; }
        public string SmtpPassword { get; set; }
        public bool SmtpUseSsl { get; set; } = true;

        // SendGrid Specific Properties
        public string SendGridApiKey { get; set; }

        // Azure Communication Services Specific Properties
        public string AzureTenantId { get; set; }
        public string AzureClientId { get; set; }
        public string AzureClientSecret { get; set; }
        public string AzureEndpoint { get; set; } = "https://communication.azure.com/";
    }
}