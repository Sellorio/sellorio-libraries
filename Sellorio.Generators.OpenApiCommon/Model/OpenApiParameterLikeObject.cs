using System.Collections.Generic;

namespace Sellorio.Generators.OpenApiCommon.Model
{
    public abstract class OpenApiParameterLikeObject : OpenApiExtensibleObject
    {
        public string Description { get; set; }
        public bool? Required { get; set; }
        public bool? Deprecated { get; set; }
        public bool? AllowEmptyValue { get; set; }
        public string Style { get; set; }
        public bool? Explode { get; set; }
        public bool? AllowReserved { get; set; }
        public OpenApiSchema Schema { get; set; }
        public object Example { get; set; }
        public IDictionary<string, OpenApiReferenceOr<OpenApiExample>> Examples { get; set; } = new Dictionary<string, OpenApiReferenceOr<OpenApiExample>>();
        public IDictionary<string, OpenApiMediaType> Content { get; set; } = new Dictionary<string, OpenApiMediaType>();
    }
}
