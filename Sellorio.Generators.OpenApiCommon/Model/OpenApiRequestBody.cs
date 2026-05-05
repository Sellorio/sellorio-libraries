using System.Collections.Generic;

namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiRequestBody : OpenApiExtensibleObject
    {
        public string Description { get; set; }
        public IDictionary<string, OpenApiMediaType> Content { get; set; } = new Dictionary<string, OpenApiMediaType>();
        public bool? Required { get; set; }
    }
}
