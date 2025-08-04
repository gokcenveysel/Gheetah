using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;

namespace Gheetah.Builder
{
    public class CustomPropertiesDataFormat : ISecureDataFormat<AuthenticationProperties>
    {
        private readonly AuthenticationPropertiesSerializer _serializer;
        private readonly IDataProtector _protector;
        private readonly ILogger<CustomPropertiesDataFormat> _logger;

        public CustomPropertiesDataFormat(
            AuthenticationPropertiesSerializer serializer,
            IDataProtectionProvider dataProtectionProvider,
            ILogger<CustomPropertiesDataFormat> logger)
        {
            _serializer = serializer;
            _protector = dataProtectionProvider.CreateProtector("OAuth.State");
            _logger = logger;
        }

        public string Protect(AuthenticationProperties data)
        {
            try
            {
                var serialized = _serializer.Serialize(data);
                var protectedData = _protector.Protect(serialized);
                return protectedData;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public string Protect(AuthenticationProperties data, string? purpose)
        {
            return Protect(data);
        }

        public AuthenticationProperties? Unprotect(string? protectedText)
        {
            if (string.IsNullOrEmpty(protectedText))
            {
                return null;
            }

            try
            {
                var serialized = _protector.Unprotect(protectedText);
                var properties = _serializer.Deserialize(serialized);
                return properties;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public AuthenticationProperties? Unprotect(string? protectedText, string? purpose)
        {
            return Unprotect(protectedText);
        }
    }
}