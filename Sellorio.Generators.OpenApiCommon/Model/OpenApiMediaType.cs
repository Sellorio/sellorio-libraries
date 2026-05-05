using System.Collections.Generic;

namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiMediaType : OpenApiExtensibleObject
    {
        public OpenApiSchema Schema { get; set; }
        public object Example { get; set; }
        public IDictionary<string, OpenApiReferenceOr<OpenApiExample>> Examples { get; set; } = new Dictionary<string, OpenApiReferenceOr<OpenApiExample>>();
        public IDictionary<string, OpenApiEncoding> Encoding { get; set; } = new Dictionary<string, OpenApiEncoding>();
    }
}
