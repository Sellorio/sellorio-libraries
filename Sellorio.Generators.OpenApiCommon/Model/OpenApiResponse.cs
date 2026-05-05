using System.Collections.Generic;

namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiResponse : OpenApiExtensibleObject
    {
        public string Description { get; set; }
        public IDictionary<string, OpenApiReferenceOr<OpenApiHeader>> Headers { get; set; } = new Dictionary<string, OpenApiReferenceOr<OpenApiHeader>>();
        public IDictionary<string, OpenApiMediaType> Content { get; set; } = new Dictionary<string, OpenApiMediaType>();
        public IDictionary<string, OpenApiReferenceOr<OpenApiLink>> Links { get; set; } = new Dictionary<string, OpenApiReferenceOr<OpenApiLink>>();
    }
}
