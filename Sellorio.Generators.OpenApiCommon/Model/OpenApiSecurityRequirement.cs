using System.Collections.Generic;

namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiSecurityRequirement : OpenApiExtensibleObject
    {
        public IDictionary<string, IList<string>> Requirements { get; set; } = new Dictionary<string, IList<string>>();
    }
}
