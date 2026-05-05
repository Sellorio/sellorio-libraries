using System.Collections.Generic;

namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiComponents : OpenApiExtensibleObject
    {
        public IDictionary<string, OpenApiSchema> Schemas { get; set; } = new Dictionary<string, OpenApiSchema>();
        public IDictionary<string, OpenApiReferenceOr<OpenApiResponse>> Responses { get; set; } = new Dictionary<string, OpenApiReferenceOr<OpenApiResponse>>();
        public IDictionary<string, OpenApiReferenceOr<OpenApiParameter>> Parameters { get; set; } = new Dictionary<string, OpenApiReferenceOr<OpenApiParameter>>();
        public IDictionary<string, OpenApiReferenceOr<OpenApiExample>> Examples { get; set; } = new Dictionary<string, OpenApiReferenceOr<OpenApiExample>>();
        public IDictionary<string, OpenApiReferenceOr<OpenApiRequestBody>> RequestBodies { get; set; } = new Dictionary<string, OpenApiReferenceOr<OpenApiRequestBody>>();
        public IDictionary<string, OpenApiReferenceOr<OpenApiHeader>> Headers { get; set; } = new Dictionary<string, OpenApiReferenceOr<OpenApiHeader>>();
        public IDictionary<string, OpenApiReferenceOr<OpenApiSecurityScheme>> SecuritySchemes { get; set; } = new Dictionary<string, OpenApiReferenceOr<OpenApiSecurityScheme>>();
        public IDictionary<string, OpenApiReferenceOr<OpenApiLink>> Links { get; set; } = new Dictionary<string, OpenApiReferenceOr<OpenApiLink>>();
        public IDictionary<string, OpenApiReferenceOr<OpenApiCallback>> Callbacks { get; set; } = new Dictionary<string, OpenApiReferenceOr<OpenApiCallback>>();
        public IDictionary<string, OpenApiReferenceOr<OpenApiPathItem>> PathItems { get; set; } = new Dictionary<string, OpenApiReferenceOr<OpenApiPathItem>>();
    }
}
