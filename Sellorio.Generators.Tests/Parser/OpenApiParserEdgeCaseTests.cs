using Sellorio.Generators.OpenApiCommon.Parser;
using Xunit;

namespace Sellorio.Generators.Tests.Parser;

public sealed class OpenApiParserEdgeCaseTests
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

        var document = OpenApiParser.ParseYaml(OpenApiYaml);

        Assert.Equal("3.1.0", document.OpenApi);
        Assert.Equal("https://json-schema.org/draft/2020-12/schema", document.JsonSchemaDialect);
        Assert.Equal("root", document.Extensions["x-root-flag"]);
        Assert.Equal("info-extension", document.Info.Extensions["x-info"]);
        Assert.Equal("prod", document.Servers[0].Variables["environment"].Default);
        Assert.Equal(2, document.Servers[0].Variables["environment"].Enum.Count);
        Assert.Equal(true, document.Paths["/items"].Extensions["x-path-flag"]);

        var operation = document.Paths["/items"].Get;
        Assert.True(operation.Deprecated);
        Assert.Single(operation.Parameters);
        Assert.False(operation.Parameters[0].Value.Explode);
        Assert.True(operation.Parameters[0].Value.AllowReserved);
        Assert.Equal("string", ((List<object>)operation.Parameters[0].Value.Schema.Type)[0]);
        Assert.Equal("null", ((List<object>)operation.Parameters[0].Value.Schema.Type)[1]);

        var responseSchema = document.Paths["/items"].Get.Responses["200"].Value.Content["application/json"].Schema;
        Assert.Equal("object", responseSchema.Type);
        Assert.Equal(false, responseSchema.AdditionalProperties.Boolean);
        Assert.Equal("string", responseSchema.UnevaluatedProperties.Schema.Type);
        Assert.Equal("schema-extension", responseSchema.Extensions["x-schema-ext"]);
        Assert.Single(responseSchema.Examples);

        var componentSchema = document.Components.Schemas["Sample"];
        Assert.Equal("https://json-schema.org/draft/2020-12/schema", componentSchema.Schema);
        Assert.Equal("urn:sample", componentSchema.Id);
        Assert.Equal("root", componentSchema.Anchor);
        Assert.Equal("node", componentSchema.DynamicAnchor);
        Assert.Equal("string", componentSchema.Defs["Child"].Type);
        Assert.Equal(2, componentSchema.DependentRequired["a"].Count);
        Assert.Equal("string", componentSchema.DependentSchemas["a"].Type);
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
        Assert.Equal("component-extension", componentSchema.Extensions["x-component-schema"]);
        var securityRequirement = Assert.Single(document.Security);
        Assert.Equal(2, securityRequirement.Requirements["bearerAuth"].Count);
        Assert.Equal("https://example.com/tags/edge", document.Tags[0].ExternalDocs.Url);
        Assert.Equal("https://example.com/docs", document.ExternalDocs.Url);
    }

    [Fact]
    public void ParseYaml_ParsesReferenceSummariesLinksCallbacksAndWebhooks()
    {
        const string OpenApiYaml = """
openapi: 3.1.0
info:
  title: Ref API
  version: 1.0.0
paths:
  /orders:
    post:
      operationId: createOrder
      responses:
        default:
          $ref: '#/components/responses/Accepted'
webhooks:
  orderCreated:
    $ref: '#/components/pathItems/OrderWebhook'
    summary: webhook summary
    description: webhook description
components:
  pathItems:
    OrderWebhook:
      post:
        operationId: onOrderCreated
        responses:
          "204":
            description: ok
  responses:
    Accepted:
      description: accepted
      links:
        followUp:
          operationId: getOrder
          parameters:
            orderId: $response.body#/id
          requestBody:
            copy: true
          description: follow up call
          server:
            url: https://example.com
  callbacks:
    OrderStatus:
      '{$request.body#/callbackUrl}':
        post:
          operationId: orderStatusChanged
          responses:
            '200':
              description: ok
  examples:
    OrderExample:
      summary: sample
      value:
        id: 1
  headers:
    CorrelationId:
      description: correlation
      schema:
        type: string
  requestBodies:
    OrderBody:
      description: order body
      required: true
      content:
        application/json:
          schema:
            $ref: '#/components/schemas/Order'
  parameters:
    CorrelationId:
      name: correlationId
      in: header
      required: true
      schema:
        type: string
  securitySchemes:
    bearerAuth:
      type: http
      scheme: bearer
      bearerFormat: jwt
  links:
    GetOrder:
      operationId: getOrder
  schemas:
    Order:
      type: object
      properties:
        id:
          type: integer
""";

        var document = OpenApiParser.ParseYaml(OpenApiYaml);

        Assert.NotNull(document.Webhooks["orderCreated"].Reference);
        Assert.Equal("#/components/pathItems/OrderWebhook", document.Webhooks["orderCreated"].Reference.Ref);
        Assert.Equal("webhook summary", document.Webhooks["orderCreated"].Reference.Summary);
        Assert.Equal("webhook description", document.Webhooks["orderCreated"].Reference.Description);

        var responseReference = document.Paths["/orders"].Post.Responses["default"];
        Assert.NotNull(responseReference.Reference);
        Assert.Equal("#/components/responses/Accepted", responseReference.Reference.Ref);

        var accepted = document.Components.Responses["Accepted"].Value;
        Assert.Equal("getOrder", accepted.Links["followUp"].Value.OperationId);
        Assert.Equal("$response.body#/id", accepted.Links["followUp"].Value.Parameters["orderId"]);
        Assert.NotNull(accepted.Links["followUp"].Value.RequestBody);
        Assert.Equal("https://example.com", accepted.Links["followUp"].Value.Server.Url);

        Assert.NotNull(document.Components.Callbacks["OrderStatus"].Value.Expressions["{$request.body#/callbackUrl}"].Post);
        Assert.Equal("sample", document.Components.Examples["OrderExample"].Value.Summary);
        Assert.Equal("correlation", document.Components.Headers["CorrelationId"].Value.Description);
        Assert.True(document.Components.RequestBodies["OrderBody"].Value.Required);
        Assert.Equal("#/components/schemas/Order", document.Components.RequestBodies["OrderBody"].Value.Content["application/json"].Schema.Ref);
        Assert.Equal("header", document.Components.Parameters["CorrelationId"].Value.In);
        Assert.Equal("http", document.Components.SecuritySchemes["bearerAuth"].Value.Type);
        Assert.Equal("getOrder", document.Components.Links["GetOrder"].Value.OperationId);
        Assert.Equal("integer", document.Components.Schemas["Order"].Properties["id"].Type);
    }

    [Fact]
    public void ParseYaml_ThrowsWhenRootIsNotMapping()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => OpenApiParser.ParseYaml("- invalid"));

        Assert.Equal("The OpenAPI YAML document root must be a mapping.", exception.Message);
    }

    [Fact]
    public void ParseYaml_ThrowsWhenYamlIsNull()
    {
        string? yaml = null;
        Assert.Throws<ArgumentNullException>(() => OpenApiParser.ParseYaml(yaml));
    }

    [Fact]
    public void ParseYaml_InferesCommonScalarShortcuts()
    {
        const string OpenApiYaml = """
openapi: 3.0.3
info: Shortcut API
servers:
  - https://example.test
paths:
  things:
    get:
      operationId: getThing
      tags: things
      security: bearerAuth
      parameters:
        name: id
        in: path
        required: yes
        schema: string
      responses:
        200: ok
tags: things
externalDocs: https://example.test/docs
""";

        var document = OpenApiParser.ParseYaml(OpenApiYaml);

        Assert.Equal("Shortcut API", document.Info.Title);
        Assert.Equal("https://example.test", Assert.Single(document.Servers).Url);
        Assert.True(document.Paths.ContainsKey("/things"));
        Assert.Equal("things", Assert.Single(document.Paths["/things"].Get.Tags));
        Assert.Single(document.Paths["/things"].Get.Parameters);
        Assert.Equal("id", document.Paths["/things"].Get.Parameters[0].Value.Name);
        Assert.True(document.Paths["/things"].Get.Parameters[0].Value.Required);
        Assert.Equal("string", document.Paths["/things"].Get.Parameters[0].Value.Schema.Type);
        Assert.Equal("ok", document.Paths["/things"].Get.Responses["200"].Value.Description);
        Assert.Equal("bearerAuth", Assert.Single(Assert.Single(document.Paths["/things"].Get.Security).Requirements.Keys));
        Assert.Equal("things", Assert.Single(document.Tags).Name);
        Assert.Equal("https://example.test/docs", document.ExternalDocs.Url);
    }

    [Fact]
    public void ParseYaml_InfersReferenceScalarsAndMediaTypeSchemaShortcuts()
    {
        const string OpenApiYaml = """
openapi: 3.0.3
info:
  title: Ref Shortcut API
  version: 1.0.0
paths:
  /things/{id}:
    parameters:
      $ref: '#/components/parameters/ThingId'
    get:
      operationId: getThing
      requestBody:
        content:
          application/json: string
      responses:
        200:
          content:
            application/json: '#/components/schemas/Thing'
components:
  parameters:
    ThingId:
      name: id
      in: path
      required: on
      schema: string
""";

        var document = OpenApiParser.ParseYaml(OpenApiYaml);

        Assert.Equal("#/components/parameters/ThingId", document.Paths["/things/{id}"].Parameters[0].Reference.Ref);
        Assert.Equal("string", document.Paths["/things/{id}"].Get.RequestBody.Value.Content["application/json"].Schema.Type);
        Assert.Equal("#/components/schemas/Thing", document.Paths["/things/{id}"].Get.Responses["200"].Value.Content["application/json"].Schema.Ref);
        Assert.True(document.Components.Parameters["ThingId"].Value.Required);
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

        var document = OpenApiParser.ParseYaml(yaml);

        Assert.Equal("3.0.3", document.OpenApi);
        Assert.Equal("Case API", document.Info.Title);
        Assert.True(document.Paths.ContainsKey("/things"));
    }

    [Theory]
    [InlineData(true, false, "tag-one")]
    [InlineData(true, true, "tag-one")]
    [InlineData(false, false, "bearerAuth")]
    [InlineData(false, true, "bearerAuth")]
    public void ParseYaml_InfersScalarOrSingleItemSequencesForCollections(bool isTags, bool useSequence, string expected)
    {
        var collectionYaml = isTags
            ? useSequence
                ? "tags:\n        - " + expected
                : "tags: " + expected
            : useSequence
                ? "security:\n        - " + expected
                : "security: " + expected;

        var yaml =
            "openapi: 3.0.3\n" +
            "info:\n" +
            "  title: Collection API\n" +
            "  version: 1.0.0\n" +
            "paths:\n" +
            "  /things:\n" +
            "    get:\n" +
            "      operationId: getThing\n" +
            "      " + collectionYaml.Replace("\n", "\n      ") + "\n" +
            "      responses:\n" +
            "        200: ok\n";

        var document = OpenApiParser.ParseYaml(yaml);
        var operation = document.Paths["/things"].Get;

        if (isTags)
        {
            Assert.Equal(expected, Assert.Single(operation.Tags));
        }
        else
        {
            Assert.Equal(expected, Assert.Single(Assert.Single(operation.Security).Requirements.Keys));
        }
    }

    [Fact]
    public void ParseYaml_InfersSingleItemSequenceContainingMapping()
    {
        const string Yaml = """
openapi: 3.0.3
info:
  title: Wrapped Mapping API
  version: 1.0.0
components:
  securitySchemes:
    bearerAuth:
      - type: http
        scheme: bearer
paths:
  /things:
    get:
      operationId: getThing
      responses:
        200: ok
""";

        var document = OpenApiParser.ParseYaml(Yaml);

        Assert.Equal("http", document.Components.SecuritySchemes["bearerAuth"].Value.Type);
        Assert.Equal("bearer", document.Components.SecuritySchemes["bearerAuth"].Value.Scheme);
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

        var document = OpenApiParser.ParseYaml(Yaml);

        var scheme = document.Components.SecuritySchemes["oauth"].Value;
        Assert.Equal("oauth2", scheme.Type);
        Assert.Equal("OAuth scheme", scheme.Description);
        Assert.Equal("https://example.test/connect/authorize", scheme.Flows.AuthorizationCode.AuthorizationUrl);
        Assert.Equal("https://example.test/connect/token", scheme.Flows.AuthorizationCode.TokenUrl);
        Assert.Equal("https://example.test/connect/refresh", scheme.Flows.AuthorizationCode.RefreshUrl);
        Assert.Equal("Read access", scheme.Flows.AuthorizationCode.Scopes["api.read"]);
        Assert.Equal("Write access", scheme.Flows.AuthorizationCode.Scopes["api.write"]);

        var schema = document.Components.Schemas["RichSchema"];
        Assert.Equal("Rich schema", schema.Title);
        Assert.Equal("Schema metadata", schema.Description);
        Assert.Equal(true, ((Dictionary<string, object>)schema.Default)["enabled"]);
        Assert.True(schema.Deprecated);
        Assert.True(schema.ReadOnly);
        Assert.False(schema.WriteOnly);
        Assert.Equal("#meta", schema.DynamicRef);
        Assert.True(schema.Vocabulary["https://example.test/vocab/core"]);
        Assert.False(schema.Vocabulary["https://example.test/vocab/validation"]);
        Assert.Equal("important", schema.Comment);
        Assert.Equal(2, schema.PrefixItems.Count);
        Assert.Equal("string", schema.PrefixItems[0].Type);
        Assert.Equal("integer", schema.PrefixItems[1].Type);
        Assert.Equal(5, schema.MaxItems);
        Assert.Equal(1, schema.MinItems);
        Assert.True(schema.UniqueItems);
        Assert.Equal(6, schema.MaxProperties);
        Assert.Equal(1, schema.MinProperties);
        Assert.Equal("string", schema.Not.Type);
        Assert.Equal("kind", schema.Discriminator.PropertyName);
        Assert.Equal("#/components/schemas/RichSchema", schema.Discriminator.Mapping["rich"]);
        Assert.Equal("rich", schema.Xml.Name);
        Assert.Equal("urn:test", schema.Xml.Namespace);
        Assert.Equal("t", schema.Xml.Prefix);
        Assert.True(schema.Xml.Attribute);
        Assert.False(schema.Xml.Wrapped);
        Assert.Equal("schema docs", schema.ExternalDocs.Description);
        Assert.Equal("https://example.test/schemas/rich", schema.ExternalDocs.Url);
        Assert.Equal(true, ((Dictionary<string, object>)schema.UnrecognizedKeywords["customKeyword"])["nested"]);
    }

    [Theory]
    [InlineData("paths: oops")]
    [InlineData("components: bad")]
    [InlineData("paths:\n  /things:\n    get:\n      responses:\n        200:\n          links: bad")]
    [InlineData("paths:\n  /things:\n    get:\n      tags:\n        name: invalid\n      responses:\n        200: ok")]
    public void ParseYaml_ThrowsForUnsupportedStructuralMismatches(string fragment)
    {
        var yaml = "openapi: 3.0.3\ninfo:\n  title: Invalid API\n  version: 1.0.0\n" + fragment;

        Assert.Throws<InvalidOperationException>(() => OpenApiParser.ParseYaml(yaml));
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

        Assert.Throws<InvalidOperationException>(() => OpenApiParser.ParseYaml(Yaml));
    }
}
