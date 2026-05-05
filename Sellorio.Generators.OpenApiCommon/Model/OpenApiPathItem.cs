using System.Collections.Generic;

namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiPathItem : OpenApiExtensibleObject
    {
        public string Ref { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public OpenApiOperation Get { get; set; }
        public OpenApiOperation Put { get; set; }
        public OpenApiOperation Post { get; set; }
        public OpenApiOperation Delete { get; set; }
        public OpenApiOperation Options { get; set; }
        public OpenApiOperation Head { get; set; }
        public OpenApiOperation Patch { get; set; }
        public OpenApiOperation Trace { get; set; }
        public IList<OpenApiServer> Servers { get; set; } = new List<OpenApiServer>();
        public IList<OpenApiReferenceOr<OpenApiParameter>> Parameters { get; set; } = new List<OpenApiReferenceOr<OpenApiParameter>>();
    }
}
