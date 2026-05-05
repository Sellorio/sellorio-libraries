using System.Collections.Generic;

namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiOAuthFlow : OpenApiExtensibleObject
    {
        public string AuthorizationUrl { get; set; }
        public string TokenUrl { get; set; }
        public string RefreshUrl { get; set; }
        public IDictionary<string, string> Scopes { get; set; } = new Dictionary<string, string>();
    }
}
