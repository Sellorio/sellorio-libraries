using System.Collections.Generic;

namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiCallback : OpenApiExtensibleObject
    {
        public IDictionary<string, OpenApiPathItem> Expressions { get; set; } = new Dictionary<string, OpenApiPathItem>();
    }
}
