using Sellorio.Generators.OpenApiCommon.Parser;
using Sellorio.Generators.Tests.Parser.Support;

namespace Sellorio.Generators.Tests.Parser;

public sealed class OpenApiParserDocumentTests
{
    [Fact]
    public void ParseYaml_ThrowsWhenYamlIsNull()
    {
        string? yaml = null;
        Assert.Throws<ArgumentNullException>(() => OpenApiParser.ParseYaml(yaml));
    }

    [Fact]
    public void ParseYaml_ThrowsWhenRootIsNotMapping()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => OpenApiParser.ParseYaml("- invalid"));
        Assert.Equal("The OpenAPI YAML document root must be a mapping.", exception.Message);
    }

    [Fact]
    public void ParseYaml_ParsesMinimalDocument()
    {
        var document = OpenApiParserTestSupport.Parse("""
openapi: 3.0.3
info:
  title: Minimal API
  version: 1.0.0
paths:
  /things:
    get:
      operationId: getThing
      responses:
        200: ok
""");

        Assert.Equal("3.0.3", document.OpenApi);
        Assert.Equal("Minimal API", document.Info.Title);
        Assert.Equal("1.0.0", document.Info.Version);
        Assert.True(document.Paths.ContainsKey("/things"));
    }

    [Theory]
    [InlineData("OpenAPI", "3.0.3")]
    [InlineData("openapi", "3.0.3")]
    [InlineData("INFO", "title: Case API\n  version: 1.0.0")]
    [InlineData("Paths", "/things:\n    get:\n      operationId: getThing\n      responses:\n        200: ok")]
    public void ParseYaml_MatchesKnownKeysCaseInsensitively(string key, string value)
    {
        var yaml = key == "INFO"
            ? "openapi: 3.0.3\n" + key + ":\n  " + value + "\npaths:\n  /things:\n    get:\n      operationId: getThing\n      responses:\n        200: ok"
            : key == "Paths"
                ? "openapi: 3.0.3\ninfo:\n  title: Case API\n  version: 1.0.0\n" + key + ":\n  " + value
                : key + ": " + value + "\ninfo:\n  title: Case API\n  version: 1.0.0\npaths:\n  /things:\n    get:\n      operationId: getThing\n      responses:\n        200: ok";

        var document = OpenApiParserTestSupport.Parse(yaml);

        Assert.Equal("3.0.3", document.OpenApi);
        Assert.Equal("Case API", document.Info.Title);
        Assert.True(document.Paths.ContainsKey("/things"));
    }

    [Fact]
    public void ParseYaml_ParsesExternalDocsAtRoot()
    {
        var document = OpenApiParserTestSupport.Parse("""
openapi: 3.0.3
info:
  title: Docs API
  version: 1.0.0
externalDocs:
  url: https://example.test/docs
  description: root docs
paths:
  /things:
    get:
      operationId: getThing
      responses:
        200: ok
""");

        Assert.Equal("https://example.test/docs", document.ExternalDocs.Url);
        Assert.Equal("root docs", document.ExternalDocs.Description);
    }

    [Fact]
    public void ParseYaml_ParsesRootExtensions()
    {
        var document = OpenApiParserTestSupport.Parse("""
openapi: 3.0.3
x-root-flag: enabled
info:
  title: Ext API
  version: 1.0.0
paths:
  /things:
    get:
      operationId: getThing
      responses:
        200: ok
""");

        Assert.Equal("enabled", document.Extensions["x-root-flag"]);
    }

    [Fact]
    public void ParseYaml_ParsesTagsCollection()
    {
        var document = OpenApiParserTestSupport.Parse("""
openapi: 3.0.3
info:
  title: Tags API
  version: 1.0.0
tags:
  - name: alpha
  - name: beta
paths:
  /things:
    get:
      operationId: getThing
      responses:
        200: ok
""");

        Assert.Equal(["alpha", "beta"], document.Tags.Select(tag => tag.Name).ToArray());
    }

    [Fact]
    public void ParseYaml_ParsesServersCollection()
    {
        var document = OpenApiParserTestSupport.Parse("""
openapi: 3.0.3
info:
  title: Servers API
  version: 1.0.0
servers:
  - url: https://one.example.test
  - url: https://two.example.test
paths:
  /things:
    get:
      operationId: getThing
      responses:
        200: ok
""");

        Assert.Equal(["https://one.example.test", "https://two.example.test"], document.Servers.Select(server => server.Url).ToArray());
    }

    [Fact]
    public void ParseYaml_ParsesComplexInlineDocumentMetadata()
    {
        var document = OpenApiParser.ParseYaml("""
openapi: 3.0.0
info:
  title: Audit Trail Service
  version: 4.0.0
servers:
  - url: https://prod.example.test
  - url: https://staging.example.test
  - url: https://dev.example.test
paths:
  /v4.0/audit-trail-entries/{id}:
    get:
      operationId: getAuditTrailEntry
      tags:
        - Audit Trail Entries
      parameters:
        - name: id
          in: path
          required: true
          schema:
            type: string
            format: uuid
            maxLength: 36
        - $ref: '#/components/parameters/acceptLanguageParam'
      responses:
        '200':
          description: ok
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/AuditTrailEntry'
  /v4.0/audit-trail-entries:
    post:
      operationId: createAuditTrailEntries
      requestBody:
        required: true
        description: Audit Trail Entries
        x-parameter-name: auditTrailEntries
        content:
          application/json:
            schema:
              type: array
              items:
                $ref: '#/components/schemas/AuditTrailEntryPost'
      responses:
        '400':
          description: bad request
          content:
            application/json:
              schema:
                oneOf:
                  - $ref: '#/components/schemas/BadRequestResponse'
                  - type: string
                    example: Can only add entries for active users.
components:
  parameters:
    acceptLanguageParam:
      name: accept-language
      in: header
      schema:
        type: string
  schemas:
    AuditTrailEntry:
      type: object
      properties:
        id:
          type: string
          format: uuid
    AuditTrailEntryPost:
      type: object
      properties:
        id:
          type: string
          format: uuid
    BadRequestResponse:
      type: object
      properties:
        message:
          type: string
""");

        Assert.Equal("3.0.0", document.OpenApi);
        Assert.Equal("Audit Trail Service", document.Info.Title);
        Assert.Equal("4.0.0", document.Info.Version);
        Assert.Equal(3, document.Servers.Count);
        Assert.True(document.Paths.ContainsKey("/v4.0/audit-trail-entries/{id}"));
        Assert.True(document.Paths.ContainsKey("/v4.0/audit-trail-entries"));
        Assert.NotEmpty(document.Components.Schemas);
        Assert.NotEmpty(document.Components.Parameters);
    }
}
