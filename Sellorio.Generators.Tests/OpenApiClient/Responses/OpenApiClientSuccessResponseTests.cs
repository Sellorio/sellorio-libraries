using Sellorio.Generators.Tests.OpenApiClient.Support;
using Sellorio.Results;
using static Sellorio.Generators.Tests.OpenApiClient.Support.OpenApiClientTestSupport;

namespace Sellorio.Generators.Tests.OpenApiClient.Responses;

public sealed class OpenApiClientSuccessResponseTests
{
    [Fact]
    public async Task GetWidget_ParsesTypedModelResponse()
    {
        using var handler = new CapturingHandler((_, _) =>
            Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, """
            {
              "id": 7,
              "name": "Widget"
            }
            """)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var widgetId = Guid.Parse("1b3f5e0b-7d74-4d52-b7cb-0f6f15d4bcf0");

        dynamic response = (await InvokeAsync(client, "GetWidget", widgetId, true, "corr-123", CancellationToken.None))!;

        Assert.True((bool)response.WasSuccess);
        Assert.Equal("ValueResult`1", response.GetType().Name);
        Assert.Equal(7, (int)response.Value.Id);
        Assert.Equal("Widget", (string)response.Value.Name);
    }

    [Fact]
    public async Task GetWidget_ReturnsDefaultModelWhenResponseBodyIsEmpty()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, string.Empty)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var widgetId = Guid.Parse("17e572bb-28bb-42ef-b0f6-0470c85d28ba");

        dynamic response = (await InvokeAsync(client, "GetWidget", widgetId, null, null, CancellationToken.None))!;

        Assert.True((bool)response.WasSuccess);
        Assert.NotNull((object)response.Value);
    }

    [Fact]
    public async Task GetWidget_ReturnsDefaultModelWhenResponseBodyIsJsonNull()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "null")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var widgetId = Guid.Parse("5279b68f-15eb-4500-945d-f561d2ebc661");

        dynamic response = (await InvokeAsync(client, "GetWidget", widgetId, null, null, CancellationToken.None))!;

        Assert.True((bool)response.WasSuccess);
        Assert.NotNull((object)response.Value);
    }

    [Fact]
    public async Task SearchWidgets_ReturnsTypedListValueResult()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "[\"a\",\"b\"]")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);

        var response = (ValueResult<IReadOnlyList<string>>)(await InvokeAsync(client, "SearchWidgets", 1, null, null, CancellationToken.None))!;

        Assert.True(response.WasSuccess);
        Assert.Equal(["a", "b"], response.Value.ToArray());
    }

    [Fact]
    public async Task SearchWidgets_ReturnsEmptyListWhenResponseBodyIsEmptyArray()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "[]")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);

        var response = (ValueResult<IReadOnlyList<string>>)(await InvokeAsync(client, "SearchWidgets", 1, null, null, CancellationToken.None))!;

        Assert.True(response.WasSuccess);
        Assert.Empty(response.Value);
    }

    [Fact]
    public async Task CreateWidget_ReturnsTypedResultForListRequestBody()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreatePlainResponse(HttpStatusCode.NoContent)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);

        dynamic response = (await InvokeAsync(client, "CreateWidget", WidgetPayload, CancellationToken.None))!;

        Assert.True((bool)response.WasSuccess);
        Assert.Equal("Result`1", response.GetType().Name);
        Assert.Equal("IReadOnlyList`1", response.GetType().GenericTypeArguments[0].Name);
    }

    [Fact]
    public async Task CreateWidgetModel_ReturnsTypedValueResultWithRequestContext()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "{\"id\":11,\"name\":\"created\"}")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var widgetModel = CreateWidgetModel("sample", 5);

        dynamic response = (await InvokeAsync(client, "CreateWidgetModel", widgetModel, CancellationToken.None))!;

        Assert.True((bool)response.WasSuccess);
        Assert.Equal("ValueResult`2", response.GetType().Name);
        Assert.Equal("WidgetModel", response.GetType().GenericTypeArguments[0].Name);
        Assert.Equal("CreateWidgetModelResponse", response.GetType().GenericTypeArguments[1].Name);
        Assert.Equal(11, (int)response.Value.Id);
        Assert.Equal("created", (string)response.Value.Name);
    }

    [Fact]
    public async Task CreateWidgetModel_ReturnsTypedValueResultWhenQuantityIsOmitted()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "{\"id\":12,\"name\":\"sample\"}")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var widgetModel = CreateWidgetModel("sample");

        dynamic response = (await InvokeAsync(client, "CreateWidgetModel", widgetModel, CancellationToken.None))!;

        Assert.True((bool)response.WasSuccess);
        Assert.Equal("ValueResult`2", response.GetType().Name);
        Assert.Equal(12, (int)response.Value.Id);
        Assert.Equal("sample", (string)response.Value.Name);
    }

    [Fact]
    public async Task GetAdminReport_ReturnsSingleTypedValueResultWithoutRequestContext()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "{\"title\":\"admin\"}")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);

        dynamic response = (await InvokeAsync(client, "GetAdminReport", CancellationToken.None))!;

        Assert.True((bool)response.WasSuccess);
        Assert.Equal("ValueResult`1", response.GetType().Name);
        Assert.Equal("admin", (string)response.Value.Title);
    }

    [Fact]
    public async Task GetEmptyReport_ReturnsInstantiatedModelForEmptyBody()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, string.Empty)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedResponseClient", handler);

        dynamic response = (await InvokeAsync(client, "GetEmptyReport", CancellationToken.None))!;

        Assert.True((bool)response.WasSuccess);
        Assert.NotNull((object)response.Value);
    }

    [Fact]
    public async Task GetEmptyReport_ReturnsInstantiatedModelForJsonNullBody()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "null")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedResponseClient", handler);

        dynamic response = (await InvokeAsync(client, "GetEmptyReport", CancellationToken.None))!;

        Assert.True((bool)response.WasSuccess);
        Assert.NotNull((object)response.Value);
    }

    [Fact]
    public async Task GetReport_ReturnsTypedPrimitiveValueResult()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "123")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedResponseClient", handler);

        var response = (ValueResult<int>)(await InvokeAsync(client, "GetReport", 42, CancellationToken.None))!;

        Assert.True(response.WasSuccess);
        Assert.Equal(123, response.Value);
    }

    [Fact]
    public async Task GetReport_ReturnsDefaultPrimitiveValueWhenBodyIsEmpty()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, string.Empty)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedResponseClient", handler);

        var response = (ValueResult<int>)(await InvokeAsync(client, "GetReport", 42, CancellationToken.None))!;

        Assert.True(response.WasSuccess);
        Assert.Equal(0, response.Value);
    }

    [Fact]
    public async Task GetReport_ThrowsJsonExceptionWhenBodyIsJsonNull()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "null")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedResponseClient", handler);

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(
            async () => await InvokeAsync(client, "GetReport", 42, CancellationToken.None));
    }

    [Fact]
    public async Task GetDefaultReport_ReturnsDefaultResponseValueResult()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "\"fallback\"")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedResponseClient", handler);

        var response = (ValueResult<string>)(await InvokeAsync(client, "GetDefaultReport", CancellationToken.None))!;

        Assert.True(response.WasSuccess);
        Assert.Equal("fallback", response.Value);
    }

    [Fact]
    public async Task GetDefaultReport_ReturnsEmptyStringWhenBodyIsEmptyJsonString()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "\"\"")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedResponseClient", handler);

        var response = (ValueResult<string>)(await InvokeAsync(client, "GetDefaultReport", CancellationToken.None))!;

        Assert.True(response.WasSuccess);
        Assert.Equal(string.Empty, response.Value);
    }

    [Fact]
    public async Task GetDefaultReport_ReturnsNullStringWhenBodyIsJsonNull()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "null")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedResponseClient", handler);

        var response = (ValueResult<string>)(await InvokeAsync(client, "GetDefaultReport", CancellationToken.None))!;

        Assert.True(response.WasSuccess);
        Assert.Null(response.Value);
    }

    [Fact]
    public async Task GetPlainReport_ReturnsSuccessfulResult()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreatePlainResponse(HttpStatusCode.NoContent)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedResponseClient", handler);

        var response = (Result)(await InvokeAsync(client, "GetPlainReport", CancellationToken.None))!;

        Assert.True(response.WasSuccess);
    }

    [Fact]
    public async Task CreateWidget_ReturnsTypedResultForBadRequestFreeSuccessOnNoContent()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreatePlainResponse(HttpStatusCode.NoContent)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);

        dynamic response = (await InvokeAsync(client, "CreateWidget", WidgetPayload, CancellationToken.None))!;

        Assert.True((bool)response.WasSuccess);
        Assert.Empty((IEnumerable<object>)response.Messages);
    }

    [Fact]
    public async Task PolymorphicClient_ReturnsObjectVariant()
    {
        using var objectResponse = CreateJsonResponse(HttpStatusCode.OK, """
        {
          "id": 12,
          "name": "Quarterly"
        }
        """);
        using var handler = new CapturingHandler((_, _) => Task.FromResult(objectResponse));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedPolymorphicClient", handler);

        dynamic response = (await InvokeAsync(client, "GetPolymorphicReport", CancellationToken.None))!;

        Assert.True((bool)response.WasSuccess);
        Assert.Equal("ValueResult`1", response.GetType().Name);
        Assert.Equal("GetPolymorphicReport200response", response.Value.GetType().Name);
        Assert.Equal(12, (int)response.Value.Id);
        Assert.Equal("Quarterly", (string)response.Value.Name);
    }

    [Fact]
    public async Task PolymorphicClient_ReturnsListVariant()
    {
        using var listResponse = CreateJsonResponse(HttpStatusCode.Created, "[\"one\",\"two\"]");
        using var handler = new CapturingHandler((_, _) => Task.FromResult(listResponse));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedPolymorphicClient", handler);

        dynamic response = (await InvokeAsync(client, "GetPolymorphicReport", CancellationToken.None))!;

        Assert.True((bool)response.WasSuccess);
        Assert.Equal("ValueResult`1", response.GetType().Name);
        Assert.Equal("GetPolymorphicReport201response", response.Value.GetType().Name);
        Assert.Equal(["one", "two"], ((IEnumerable<string>)response.Value).ToArray());
    }

    [Fact]
    public async Task PolymorphicClient_ReturnsEmptyListVariantWhenResponseBodyIsEmptyArray()
    {
        using var listResponse = CreateJsonResponse(HttpStatusCode.Created, "[]");
        using var handler = new CapturingHandler((_, _) => Task.FromResult(listResponse));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedPolymorphicClient", handler);

        dynamic response = (await InvokeAsync(client, "GetPolymorphicReport", CancellationToken.None))!;

        Assert.True((bool)response.WasSuccess);
        Assert.Equal("GetPolymorphicReport201response", response.Value.GetType().Name);
        Assert.Empty(((IEnumerable<string>)response.Value).ToArray());
    }

    [Fact]
    public async Task PolymorphicClient_ReturnsInterfaceTypedValueResultForObjectVariant()
    {
        using var objectResponse = CreateJsonResponse(HttpStatusCode.OK, """
        {
          "id": 4,
          "name": "Monthly"
        }
        """);
        using var handler = new CapturingHandler((_, _) => Task.FromResult(objectResponse));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedPolymorphicClient", handler);

        dynamic response = (await InvokeAsync(client, "GetPolymorphicReport", CancellationToken.None))!;

        Assert.Equal("IGetPolymorphicReportResponse", response.GetType().GenericTypeArguments[0].Name);
        Assert.Equal("GetPolymorphicReport200response", response.Value.GetType().Name);
    }

    [Fact]
    public async Task PolymorphicClient_ReturnsInterfaceTypedValueResultForListVariant()
    {
        using var listResponse = CreateJsonResponse(HttpStatusCode.Created, "[\"x\"]");
        using var handler = new CapturingHandler((_, _) => Task.FromResult(listResponse));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedPolymorphicClient", handler);

        dynamic response = (await InvokeAsync(client, "GetPolymorphicReport", CancellationToken.None))!;

        Assert.Equal("IGetPolymorphicReportResponse", response.GetType().GenericTypeArguments[0].Name);
        Assert.Equal("GetPolymorphicReport201response", response.Value.GetType().Name);
    }
}
