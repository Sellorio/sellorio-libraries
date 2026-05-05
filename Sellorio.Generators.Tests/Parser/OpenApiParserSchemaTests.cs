using Sellorio.Generators.OpenApiCommon.Parser;
using Sellorio.Generators.Tests.Parser.Support;

namespace Sellorio.Generators.Tests.Parser;

public sealed class OpenApiParserSchemaTests
{
    [Fact]
    public void ParseYaml_ParsesOpenApi31AndSchemaKeywordsAndExtensions()
    {
        const string OpenApiYaml = """
openapi: 3.1.0
jsonSchemaDialect: https://json-schema.org/draft/2020-12/schema
x-root-flag: root
info:
  title: Edge API
  version: 1.2.3
  summary: Summary text
  x-info: info-extension
servers:
  - url: https://{environment}.example.com
    description: primary
    variables:
      environment:
        default: prod
        enum: [prod, staging]
paths:
  /items:
    x-path-flag: true
    get:
      operationId: listItems
      deprecated: true
      parameters:
        - name: filter
          in: query
          explode: false
          allowReserved: true
          schema:
            type: [string, "null"]
            minLength: 1
            pattern: ^[a-z]+$
      responses:
        "200":
          description: ok
          headers:
            X-Rate-Limit:
              description: calls remaining
              schema:
                type: integer
          content:
            application/json:
              schema:
                type: object
                required: [id]
                properties:
                  id:
                    type: string
                  tag:
                    type: string
                additionalProperties: false
                unevaluatedProperties:
                  type: string
                examples:
                  - sample
                x-schema-ext: schema-extension
components:
  schemas:
    Sample:
      $schema: https://json-schema.org/draft/2020-12/schema
      $id: urn:sample
      $anchor: root
      $dynamicAnchor: node
      $defs:
        Child:
          type: string
      type: object
      dependentRequired:
        a: [b, c]
      dependentSchemas:
        a:
          type: string
      propertyNames:
        pattern: ^[A-Z]+$
      patternProperties:
        ^x-:
          type: integer
      unevaluatedItems: false
      contains:
        type: integer
      minContains: 1
      maxContains: 3
      if:
        type: string
      then:
        minLength: 3
      else:
        type: integer
      contentEncoding: base64
      contentMediaType: application/json
      contentSchema:
        type: object
      const: fixed
      enum: [fixed, alt]
      x-component-schema: component-extension
security:
  - bearerAuth: [read, write]
tags:
  - name: edge
    externalDocs:
      url: https://example.com/tags/edge
externalDocs:
  url: https://example.com/docs
""";

        var document = OpenApiParserTestSupport.Parse(OpenApiYaml);
        var responseSchema = document.Paths["/items"].Get.Responses["200"].Value.Content["application/json"].Schema;
        var componentSchema = document.Components.Schemas["Sample"];

        Assert.Equal("3.1.0", document.OpenApi);
        Assert.Equal("https://json-schema.org/draft/2020-12/schema", document.JsonSchemaDialect);
        Assert.Equal(false, responseSchema.AdditionalProperties.Boolean);
        Assert.Equal("string", responseSchema.UnevaluatedProperties.Schema.Type);
        Assert.Equal("schema-extension", responseSchema.Extensions["x-schema-ext"]);
        Assert.Equal("urn:sample", componentSchema.Id);
        Assert.Equal("root", componentSchema.Anchor);
        Assert.Equal("node", componentSchema.DynamicAnchor);
        Assert.Equal("string", componentSchema.Defs["Child"].Type);
        Assert.Equal("^[A-Z]+$", componentSchema.PropertyNames.Pattern);
        Assert.Equal("integer", componentSchema.PatternProperties["^x-"].Type);
        Assert.Equal(false, componentSchema.UnevaluatedItems.Boolean);
        Assert.Equal("integer", componentSchema.Contains.Type);
        Assert.Equal(1, componentSchema.MinContains);
        Assert.Equal(3, componentSchema.MaxContains);
        Assert.Equal("string", componentSchema.If.Type);
        Assert.Equal(3, componentSchema.Then.MinLength);
        Assert.Equal("integer", componentSchema.Else.Type);
        Assert.Equal("base64", componentSchema.ContentEncoding);
        Assert.Equal("application/json", componentSchema.ContentMediaType);
        Assert.Equal("object", componentSchema.ContentSchema.Type);
        Assert.Equal("fixed", componentSchema.Const);
        Assert.Equal(2, componentSchema.Enum.Count);
    }

    [Fact]
    public void ParseYaml_ParsesSchemaMetadataAndOAuthFlows()
    {
        const string Yaml = """
openapi: 3.1.0
info:
  title: Metadata API
  version: 1.0.0
components:
  securitySchemes:
    oauth:
      type: oauth2
      description: OAuth scheme
      flows:
        authorizationCode:
          authorizationUrl: https://example.test/connect/authorize
          tokenUrl: https://example.test/connect/token
          refreshUrl: https://example.test/connect/refresh
          scopes:
            api.read: Read access
            api.write: Write access
  schemas:
    RichSchema:
      type: object
      title: Rich schema
      description: Schema metadata
      default:
        enabled: true
      deprecated: true
      readOnly: true
      writeOnly: false
      $dynamicRef: '#meta'
      $vocabulary:
        https://example.test/vocab/core: true
        https://example.test/vocab/validation: false
      $comment: important
      prefixItems:
        - type: string
        - type: integer
      maxItems: 5
      minItems: 1
      uniqueItems: true
      maxProperties: 6
      minProperties: 1
      not:
        type: string
      discriminator:
        propertyName: kind
        mapping:
          rich: '#/components/schemas/RichSchema'
      xml:
        name: rich
        namespace: urn:test
        prefix: t
        attribute: true
        wrapped: false
      externalDocs:
        description: schema docs
        url: https://example.test/schemas/rich
      customKeyword:
        nested: true
paths:
  /metadata:
    get:
      operationId: getMetadata
      responses:
        200: ok
""";

        var document = OpenApiParserTestSupport.Parse(Yaml);
        var scheme = document.Components.SecuritySchemes["oauth"].Value;
        var schema = document.Components.Schemas["RichSchema"];

        Assert.Equal("oauth2", scheme.Type);
        Assert.Equal("OAuth scheme", scheme.Description);
        Assert.Equal("https://example.test/connect/authorize", scheme.Flows.AuthorizationCode.AuthorizationUrl);
        Assert.Equal("Read access", scheme.Flows.AuthorizationCode.Scopes["api.read"]);
        Assert.Equal("Rich schema", schema.Title);
        Assert.Equal("Schema metadata", schema.Description);
        Assert.True(schema.Deprecated);
        Assert.True(schema.ReadOnly);
        Assert.False(schema.WriteOnly);
        Assert.Equal("#meta", schema.DynamicRef);
        Assert.Equal("important", schema.Comment);
        Assert.Equal(2, schema.PrefixItems.Count);
        Assert.Equal("kind", schema.Discriminator.PropertyName);
        Assert.Equal("rich", schema.Xml.Name);
        Assert.Equal("schema docs", schema.ExternalDocs.Description);
        Assert.Equal(true, ((Dictionary<string, object>)schema.UnrecognizedKeywords["customKeyword"])["nested"]);
    }

    [Theory]
    [InlineData("integer")]
    [InlineData("number")]
    [InlineData("boolean")]
    public void ParseYaml_ParsesPrimitiveSchemaTypes(string type)
    {
        var document = OpenApiParserTestSupport.ParseWithComponents(
            "schemas:\n" +
            "  Primitive:\n" +
            "    type: " + type + "\n");

        Assert.Equal(type, document.Components.Schemas["Primitive"].Type);
    }

    [Fact]
    public void ParseYaml_ParsesArraySchemaItems()
    {
        var document = OpenApiParserTestSupport.ParseWithComponents("""
schemas:
  Values:
    type: array
    items:
      type: string
""");

        Assert.Equal("array", document.Components.Schemas["Values"].Type);
        Assert.Equal("string", document.Components.Schemas["Values"].Items.Type);
    }

    [Fact]
    public void ParseYaml_ParsesObjectSchemaPropertiesAndRequired()
    {
        var document = OpenApiParserTestSupport.ParseWithComponents("""
schemas:
  Widget:
    type: object
    required: [id, name]
    properties:
      id:
        type: integer
      name:
        type: string
""");

        var schema = document.Components.Schemas["Widget"];
        Assert.Equal(["id", "name"], schema.Required.ToArray());
        Assert.Equal("integer", schema.Properties["id"].Type);
        Assert.Equal("string", schema.Properties["name"].Type);
    }

    [Fact]
    public void ParseYaml_ParsesOneOfAnyOfAndAllOf()
    {
        var document = OpenApiParserTestSupport.ParseWithComponents("""
schemas:
  Composite:
    oneOf:
      - type: string
    anyOf:
      - type: integer
    allOf:
      - type: object
""");

        var schema = document.Components.Schemas["Composite"];
        Assert.Equal("string", schema.OneOf[0].Type);
        Assert.Equal("integer", schema.AnyOf[0].Type);
        Assert.Equal("object", schema.AllOf[0].Type);
    }

    [Fact]
    public void ParseYaml_ParsesDictionaryValueNodes()
    {
        var document = OpenApiParserTestSupport.ParseWithComponents("""
examples:
  Example:
    summary: sample
    value:
      id: 1
      name: widget
""");

        var value = (Dictionary<string, object>)document.Components.Examples["Example"].Value.Value;
        Assert.Equal(1, value["id"]);
        Assert.Equal("widget", value["name"]);
    }

    [Fact]
    public void ParseYaml_ParsesInlineRequestBodiesAndComposedSchemas()
    {
        var document = OpenApiParser.ParseYaml("""
openapi: 3.0.0
info:
  title: Audit Trail Service
  version: 4.0.0
paths:
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
  schemas:
    AuditTrailEntryPost:
      type: object
      properties:
        id:
          type: string
    BadRequestResponse:
      type: object
      properties:
        message:
          type: string
""");
        var createOperation = document.Paths["/v4.0/audit-trail-entries"].Post;
        var errorSchema = createOperation.Responses["400"].Value.Content["application/json"].Schema;

        Assert.True(createOperation.RequestBody.Value.Required);
        Assert.Equal("Audit Trail Entries", createOperation.RequestBody.Value.Description);
        Assert.Equal("auditTrailEntries", createOperation.RequestBody.Value.Extensions["x-parameter-name"]);
        Assert.Equal("array", createOperation.RequestBody.Value.Content["application/json"].Schema.Type);
        Assert.Equal("#/components/schemas/AuditTrailEntryPost", createOperation.RequestBody.Value.Content["application/json"].Schema.Items.Ref);
        Assert.Equal(2, errorSchema.OneOf.Count);
        Assert.Equal("#/components/schemas/BadRequestResponse", errorSchema.OneOf[0].Ref);
        Assert.Equal("string", errorSchema.OneOf[1].Type);
        Assert.Contains("Can only add entries", (string)errorSchema.OneOf[1].Example);
    }
}
