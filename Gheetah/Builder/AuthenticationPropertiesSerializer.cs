using Microsoft.AspNetCore.Authentication;
using System.Text.Json;

namespace Gheetah.Builder
{
    public class AuthenticationPropertiesSerializer
    {
        public string Serialize(AuthenticationProperties properties)
        {
            return JsonSerializer.Serialize(properties.Items);
        }

        public AuthenticationProperties Deserialize(string serialized)
        {
            var items = JsonSerializer.Deserialize<Dictionary<string, string>>(serialized);
            return new AuthenticationProperties(items);
        }
    }
}