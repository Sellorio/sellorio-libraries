using Sellorio.Generators.OpenApiCommon.Parser;
using Xunit;

namespace Sellorio.Generators.Tests.Parser;

public sealed class OpenApiParserRealWorldDocumentTests
{
    private static string ReadDefinitionYaml()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Definition", "v4.0.yaml");
        return File.ReadAllText(path);
    }

    [Fact]
    public void ParseYaml_ParsesRealWorldDefinition()
    {
        var document = OpenApiParser.ParseYaml(ReadDefinitionYaml());

        Assert.Equal("3.0.0", document.OpenApi);
        Assert.NotNull(document.Info);
        Assert.Equal("Audit Trail Service", document.Info.Title);
        Assert.Equal("4.0.0", document.Info.Version);
        Assert.Equal(3, document.Servers.Count);
        Assert.NotNull(document.Paths);
        Assert.True(document.Paths.ContainsKey("/v4.0/audit-trail-entries/{id}"));
        Assert.True(document.Paths.ContainsKey("/v4.0/audit-trail-entries"));
        Assert.NotNull(document.Components);
        Assert.NotEmpty(document.Components.Schemas);
        Assert.NotEmpty(document.Components.Parameters);
    }

    [Fact]
    public void ParseYaml_ParsesOperationsParametersAndReferences()
    {
        var document = OpenApiParser.ParseYaml(ReadDefinitionYaml());
        var operation = document.Paths["/v4.0/audit-trail-entries/{id}"].Get;

        Assert.NotNull(operation);
        Assert.Equal("getAuditTrailEntry", operation.OperationId);
        Assert.Equal("Audit Trail Entries", Assert.Single(operation.Tags));
        Assert.Equal(2, operation.Parameters.Count);
        Assert.Equal("id", operation.Parameters[0].Value.Name);
        Assert.Equal("path", operation.Parameters[0].Value.In);
        Assert.True(operation.Parameters[0].Value.Required);
        Assert.Equal("string", operation.Parameters[0].Value.Schema.Type);
        Assert.Equal("uuid", operation.Parameters[0].Value.Schema.Format);
        Assert.Equal(36, operation.Parameters[0].Value.Schema.MaxLength);
        Assert.Null(operation.Parameters[1].Value);
        Assert.NotNull(operation.Parameters[1].Reference);
        Assert.Equal("#/components/parameters/acceptLanguageParam", operation.Parameters[1].Reference.Ref);
        Assert.NotNull(operation.Responses["200"].Value.Content["application/json"].Schema);
        Assert.Equal("#/components/schemas/AuditTrailEntry", operation.Responses["200"].Value.Content["application/json"].Schema.Ref);
    }

    [Fact]
    public void ParseYaml_ParsesRequestBodiesAndComposedSchemas()
    {
        var document = OpenApiParser.ParseYaml(ReadDefinitionYaml());
        var createOperation = document.Paths["/v4.0/audit-trail-entries"].Post;
        var errorSchema = createOperation.Responses["400"].Value.Content["application/json"].Schema;

        Assert.NotNull(createOperation.RequestBody);
        Assert.NotNull(createOperation.RequestBody.Value);
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
