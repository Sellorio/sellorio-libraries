using System;
using Sellorio.Generators.OpenApiCommon.Model;

namespace Sellorio.Generators.OpenApiCommon.Parser
{
    public static class OpenApiParser
    {
        public static OpenApiDocument ParseYaml(string yaml)
        {
            if (yaml == null)
            {
                throw new ArgumentNullException(nameof(yaml));
            }

            return OpenApiYamlMapper.Parse(yaml);
        }
    }
}
