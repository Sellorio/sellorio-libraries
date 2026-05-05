using System.Collections.Generic;

namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiLink : OpenApiExtensibleObject
    {
        public string OperationRef { get; set; }
        public string OperationId { get; set; }
        public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public object RequestBody { get; set; }
        public string Description { get; set; }
        public OpenApiServer Server { get; set; }
    }
}
