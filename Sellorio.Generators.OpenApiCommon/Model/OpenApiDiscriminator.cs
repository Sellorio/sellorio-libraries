using System.Collections.Generic;

namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiDiscriminator : OpenApiExtensibleObject
    {
        public string PropertyName { get; set; }
        public IDictionary<string, string> Mapping { get; set; } = new Dictionary<string, string>();
    }
}
