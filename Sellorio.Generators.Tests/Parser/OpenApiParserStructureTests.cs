using Sellorio.Generators.Tests.Parser.Support;

namespace Sellorio.Generators.Tests.Parser;

public sealed class OpenApiParserStructureTests
{
    [Fact]
    public void ParseYaml_InfersCommonScalarShortcuts()
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

        var document = OpenApiParserTestSupport.Parse(OpenApiYaml);

        Assert.Equal("Shortcut API", document.Info.Title);
        Assert.Equal("https://example.test", Assert.Single(document.Servers).Url);
        Assert.True(document.Paths.ContainsKey("/things"));
        Assert.Equal("things", Assert.Single(document.Paths["/things"].Get.Tags));
        Assert.Equal("bearerAuth", Assert.Single(Assert.Single(document.Paths["/things"].Get.Security).Requirements.Keys));
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

        var document = OpenApiParserTestSupport.Parse(OpenApiYaml);

        Assert.Equal("#/components/parameters/ThingId", document.Paths["/things/{id}"].Parameters[0].Reference.Ref);
        Assert.Equal("string", document.Paths["/things/{id}"].Get.RequestBody.Value.Content["application/json"].Schema.Type);
        Assert.Equal("#/components/schemas/Thing", document.Paths["/things/{id}"].Get.Responses["200"].Value.Content["application/json"].Schema.Ref);
    }

    [Theory]
    [InlineData(true, false, "tag-one")]
    [InlineData(true, true, "tag-one")]
    [InlineData(false, false, "bearerAuth")]
    [InlineData(false, true, "bearerAuth")]
    public void ParseYaml_InfersScalarOrSingleItemSequencesForCollections(bool isTags, bool useSequence, string expected)
    {
        var collectionYaml = isTags
            ? useSequence ? "tags:\n        - " + expected : "tags: " + expected
            : useSequence ? "security:\n        - " + expected : "security: " + expected;

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

        var document = OpenApiParserTestSupport.Parse(yaml);
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

        var document = OpenApiParserTestSupport.Parse(Yaml);

        Assert.Equal("http", document.Components.SecuritySchemes["bearerAuth"].Value.Type);
        Assert.Equal("bearer", document.Components.SecuritySchemes["bearerAuth"].Value.Scheme);
    }

    [Fact]
    public void ParseYaml_ParsesGetOperationWithParametersAndResponses()
    {
        var document = OpenApiParserTestSupport.ParseWithPaths("""
/things/{id}:
  get:
    operationId: getThing
    parameters:
      - name: id
        in: path
        required: true
        schema:
          type: string
      - name: includeDetails
        in: query
        schema:
          type: boolean
    responses:
      '200':
        description: ok
""");

        var operation = document.Paths["/things/{id}"].Get;
        Assert.Equal("getThing", operation.OperationId);
        Assert.Equal(2, operation.Parameters.Count);
        Assert.Equal("id", operation.Parameters[0].Value.Name);
        Assert.Equal("query", operation.Parameters[1].Value.In);
    }

    [Fact]
    public void ParseYaml_ParsesPostOperationWithRequestBody()
    {
        var document = OpenApiParserTestSupport.ParseWithPaths("""
/things:
  post:
    operationId: createThing
    requestBody:
      required: true
      description: body
      content:
        application/json:
          schema:
            type: string
    responses:
      '204':
        description: done
""");

        var operation = document.Paths["/things"].Post;
        Assert.True(operation.RequestBody.Value.Required);
        Assert.Equal("body", operation.RequestBody.Value.Description);
        Assert.Equal("string", operation.RequestBody.Value.Content["application/json"].Schema.Type);
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

        var document = OpenApiParserTestSupport.Parse(OpenApiYaml);

        Assert.Equal("#/components/pathItems/OrderWebhook", document.Webhooks["orderCreated"].Reference.Ref);
        Assert.Equal("getOrder", document.Components.Responses["Accepted"].Value.Links["followUp"].Value.OperationId);
        Assert.Equal("http", document.Components.SecuritySchemes["bearerAuth"].Value.Type);
        Assert.Equal("integer", document.Components.Schemas["Order"].Properties["id"].Type);
    }
}
