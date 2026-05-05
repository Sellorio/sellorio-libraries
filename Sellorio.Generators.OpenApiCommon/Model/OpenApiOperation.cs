using System.Collections.Generic;

namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiOperation : OpenApiExtensibleObject
    {
        public IList<string> Tags { get; set; } = new List<string>();
        public string Summary { get; set; }
        public string Description { get; set; }
        public OpenApiExternalDocumentation ExternalDocs { get; set; }
        public string OperationId { get; set; }
        public IList<OpenApiReferenceOr<OpenApiParameter>> Parameters { get; set; } = new List<OpenApiReferenceOr<OpenApiParameter>>();
        public OpenApiReferenceOr<OpenApiRequestBody> RequestBody { get; set; }
        public OpenApiResponses Responses { get; set; }
        public IDictionary<string, OpenApiReferenceOr<OpenApiCallback>> Callbacks { get; set; } = new Dictionary<string, OpenApiReferenceOr<OpenApiCallback>>();
        public bool? Deprecated { get; set; }
        public IList<OpenApiSecurityRequirement> Security { get; set; } = new List<OpenApiSecurityRequirement>();
        public IList<OpenApiServer> Servers { get; set; } = new List<OpenApiServer>();
    }
}
