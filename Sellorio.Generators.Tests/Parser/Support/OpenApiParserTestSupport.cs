using Sellorio.Generators.OpenApiCommon.Model;
using Sellorio.Generators.OpenApiCommon.Parser;

namespace Sellorio.Generators.Tests.Parser.Support;

internal static class OpenApiParserTestSupport
{
    public static OpenApiDocument Parse(string yaml)
    {
        return OpenApiParser.ParseYaml(yaml);
    }

    public static OpenApiDocument ParseWithPaths(string pathsYaml)
    {
        return Parse(
            "openapi: 3.0.3\n" +
            "info:\n" +
            "  title: Test API\n" +
            "  version: 1.0.0\n" +
            "paths:\n" + Indent(pathsYaml, 2));
    }

    public static OpenApiDocument ParseWithComponents(string componentsYaml)
    {
        return Parse(
            "openapi: 3.0.3\n" +
            "info:\n" +
            "  title: Test API\n" +
            "  version: 1.0.0\n" +
            "paths:\n" +
            "  /things:\n" +
            "    get:\n" +
            "      operationId: getThing\n" +
            "      responses:\n" +
            "        200: ok\n" +
            "components:\n" + Indent(componentsYaml, 2));
    }

    public static string Indent(string value, int spaces)
    {
        var indent = new string(' ', spaces);
        return indent + value.Replace("\n", "\n" + indent);
    }
}
