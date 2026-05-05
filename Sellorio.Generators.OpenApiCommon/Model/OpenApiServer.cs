using System.Collections.Generic;

namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiServer : OpenApiExtensibleObject
    {
        public string Url { get; set; }
        public string Description { get; set; }
        public IDictionary<string, OpenApiServerVariable> Variables { get; set; } = new Dictionary<string, OpenApiServerVariable>();
    }
}
