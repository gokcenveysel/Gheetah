using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Gheetah.Models
{
    public class AuthenticationResult
    {
        public ClaimsPrincipal Principal { get; }
        public AuthenticationProperties Properties { get; }
        public string Scheme { get; }

        public AuthenticationResult(ClaimsPrincipal principal, AuthenticationProperties properties, string scheme)
        {
            Principal = principal;
            Properties = properties;
            Scheme = scheme;
        }
    }
}
