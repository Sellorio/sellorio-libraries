using System.Collections.Generic;

namespace Sellorio.Generators.OpenApiCommon.Model
{
    public sealed class OpenApiServerVariable : OpenApiExtensibleObject
    {
        public IList<string> Enum { get; set; } = new List<string>();
        public string Default { get; set; }
        public string Description { get; set; }
    }
}
