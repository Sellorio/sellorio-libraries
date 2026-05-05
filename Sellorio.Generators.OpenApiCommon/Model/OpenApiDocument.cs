using System.Collections.Generic;

namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiDocument : OpenApiExtensibleObject
    {
        public string OpenApi { get; set; }
        public OpenApiInfo Info { get; set; }
        public string JsonSchemaDialect { get; set; }
        public IList<OpenApiServer> Servers { get; set; } = new List<OpenApiServer>();
        public OpenApiPaths Paths { get; set; }
        public IDictionary<string, OpenApiReferenceOr<OpenApiPathItem>> Webhooks { get; set; } = new Dictionary<string, OpenApiReferenceOr<OpenApiPathItem>>();
        public OpenApiComponents Components { get; set; }
        public IList<OpenApiSecurityRequirement> Security { get; set; } = new List<OpenApiSecurityRequirement>();
        public IList<OpenApiTag> Tags { get; set; } = new List<OpenApiTag>();
        public OpenApiExternalDocumentation ExternalDocs { get; set; }
    }
}
