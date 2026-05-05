using System.Collections.Generic;

namespace Sellorio.Generators.OpenApiCommon.Model
{
    public abstract class OpenApiExtensibleObject
    {
        public IDictionary<string, object> Extensions { get; set; } = new Dictionary<string, object>();
    }
}
