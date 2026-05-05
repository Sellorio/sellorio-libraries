using System.Collections.Generic;

namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiEncoding : OpenApiExtensibleObject
    {
        public string ContentType { get; set; }
        public IDictionary<string, OpenApiReferenceOr<OpenApiHeader>> Headers { get; set; } = new Dictionary<string, OpenApiReferenceOr<OpenApiHeader>>();
        public string Style { get; set; }
        public bool? Explode { get; set; }
        public bool? AllowReserved { get; set; }
    }
}
