using Sellorio.Generators.Tests.Parser.Support;

namespace Sellorio.Generators.Tests.Parser;

public sealed class OpenApiParserFailureAndScalarTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("yes", true)]
    [InlineData("no", false)]
    [InlineData("on", true)]
    [InlineData("off", false)]
    public void ParseYaml_ParsesBooleanScalars(string value, bool expected)
    {
        var document = OpenApiParserTestSupport.ParseWithPaths($"""
/things:
  get:
    operationId: getThing
    deprecated: {value}
    responses:
      '200':
        description: ok
""");

        Assert.Equal(expected, document.Paths["/things"].Get.Deprecated);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("42", 42)]
    [InlineData("0", 0)]
    public void ParseYaml_ParsesIntegerScalars(string value, int expected)
    {
        var document = OpenApiParserTestSupport.ParseWithComponents($"""
schemas:
  Counted:
    type: object
    maxProperties: {value}
""");

        Assert.Equal(expected, document.Components.Schemas["Counted"].MaxProperties);
    }

    [Theory]
    [InlineData("3.14", 3.14)]
    [InlineData("0.5", 0.5)]
    public void ParseYaml_ParsesDecimalScalars(string value, double expected)
    {
        var document = OpenApiParserTestSupport.ParseWithComponents($"""
schemas:
  Numeric:
    type: number
    minimum: {value}
""");

        Assert.Equal((decimal)expected, document.Components.Schemas["Numeric"].Minimum);
    }

    [Fact]
    public void ParseYaml_ParsesNullLikeOptionalValuesAsNullWhenMissing()
    {
        var document = OpenApiParserTestSupport.ParseWithPaths("""
/things:
  get:
    operationId: getThing
    responses:
      '200':
        description: ok
""");

        Assert.Null(document.Paths["/things"].Get.Summary);
        Assert.Null(document.Paths["/things"].Get.Description);
    }

    [Theory]
    [InlineData("paths: oops")]
    [InlineData("components: bad")]
    [InlineData("paths:\n  /things:\n    get:\n      responses:\n        200:\n          links: bad")]
    [InlineData("paths:\n  /things:\n    get:\n      tags:\n        name: invalid\n      responses:\n        200: ok")]
    public void ParseYaml_ThrowsForUnsupportedStructuralMismatches(string fragment)
    {
        var yaml = "openapi: 3.0.3\ninfo:\n  title: Invalid API\n  version: 1.0.0\n" + fragment;
        Assert.Throws<InvalidOperationException>(() => OpenApiParserTestSupport.Parse(yaml));
    }

    [Fact]
    public void ParseYaml_ThrowsForScalarSequenceContainingMappingsWhenScalarsRequired()
    {
        const string Yaml = """
openapi: 3.0.3
info:
  title: Invalid Scalar API
  version: 1.0.0
paths:
  /things:
    get:
      operationId: getThing
      tags:
        - name: invalid
      responses:
        200: ok
""";

        Assert.Throws<InvalidOperationException>(() => OpenApiParserTestSupport.Parse(Yaml));
    }

    [Theory]
    [InlineData("openapi: 3.0.3\ninfo:\n  title: Test\n  version: 1.0.0\npaths:\n  - invalid")]
    [InlineData("openapi: 3.0.3\ninfo:\n  title: Test\n  version: 1.0.0\ncomponents:\n  schemas: nope\npaths:\n  /things:\n    get:\n      operationId: getThing\n      responses:\n        200: ok")]
    [InlineData("openapi: 3.0.3\ninfo:\n  title: Test\n  version: 1.0.0\npaths:\n  /things:\n    get:\n      operationId: getThing\n      parameters: nope\n      responses:\n        200: ok")]
    public void ParseYaml_RejectsClearlyInvalidStructuralShapes(string yaml)
    {
        Assert.Throws<InvalidOperationException>(() => OpenApiParserTestSupport.Parse(yaml));
    }
}
