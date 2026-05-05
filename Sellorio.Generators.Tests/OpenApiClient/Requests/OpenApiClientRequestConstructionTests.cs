using Sellorio.Generators.Tests.OpenApiClient.Support;
using static Sellorio.Generators.Tests.OpenApiClient.Support.OpenApiClientTestSupport;

namespace Sellorio.Generators.Tests.OpenApiClient.Requests;

public sealed class OpenApiClientRequestConstructionTests
{
    [Fact]
    public async Task GetWidget_SendsExpectedPathQueryAndHeader()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "{}")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var widgetId = Guid.Parse("1b3f5e0b-7d74-4d52-b7cb-0f6f15d4bcf0");

        await InvokeAsync(client, "GetWidget", widgetId, true, "corr-123", CancellationToken.None);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal($"https://example.test/widgets/{widgetId:D}?includeDetails=True", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("corr-123", Assert.Single(handler.LastRequest.Headers["X-Correlation-ID"]));
    }

    [Fact]
    public async Task GetWidget_OmitsUnsetOptionalQueryAndHeader()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "{}")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var widgetId = Guid.Parse("c3f8b6f3-3b43-4638-a5cb-5c12911bb87a");

        await InvokeAsync(client, "GetWidget", widgetId, null, null, CancellationToken.None);

        Assert.Equal($"https://example.test/widgets/{widgetId:D}", handler.LastRequest!.RequestUri!.ToString());
        Assert.False(handler.LastRequest.Headers.ContainsKey("X-Correlation-ID"));
    }

    [Fact]
    public async Task GetWidget_EncodesGuidUsingDFormat()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "{}")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var widgetId = Guid.Parse("AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE");

        await InvokeAsync(client, "GetWidget", widgetId, null, null, CancellationToken.None);

        Assert.EndsWith("/widgets/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", handler.LastRequest!.RequestUri!.AbsoluteUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetWidget_SendsNoBodyOrContentType()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "{}")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var widgetId = Guid.Parse("0da2d4c7-b393-458e-9417-0e4d69c39d24");

        await InvokeAsync(client, "GetWidget", widgetId, null, null, CancellationToken.None);

        Assert.Null(handler.LastRequest!.Body);
        Assert.Null(handler.LastRequest.ContentType);
    }

    [Fact]
    public async Task CreateWidget_SerializesListBodyAsJson()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreatePlainResponse(HttpStatusCode.NoContent)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);

        await InvokeAsync(client, "CreateWidget", WidgetPayload, CancellationToken.None);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://example.test/widgets", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("application/json; charset=utf-8", handler.LastRequest.ContentType);
        Assert.Equal("[\"first\",\"second\"]", handler.LastRequest.Body);
    }

    [Fact]
    public async Task CreateWidget_SendsNoCustomHeaders()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreatePlainResponse(HttpStatusCode.NoContent)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);

        await InvokeAsync(client, "CreateWidget", WidgetPayload, CancellationToken.None);

        Assert.DoesNotContain(handler.LastRequest!.Headers.Keys, header => header.Equals("X-Correlation-ID", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(handler.LastRequest.Headers.Keys, header => header.Equals("X-Request-ID", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchWidgets_SerializesRequiredAndOptionalParametersUsingInvariantFormat()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "[]")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var createdAfter = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero).AddTicks(6789);

        await InvokeAsync(client, "SearchWidgets", 99, createdAfter, 1234.5m, CancellationToken.None);

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal("99", Assert.Single(handler.LastRequest.Headers["X-Request-ID"]));
        Assert.Contains("createdAfter=2025-01-02T03%3A04%3A05.0006789%2B00%3A00", handler.LastRequest.RequestUri!.Query, StringComparison.Ordinal);
        Assert.Contains("score=1234.5", handler.LastRequest.RequestUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SearchWidgets_OmitsOptionalQueryParametersWhenNull()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "[]")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);

        await InvokeAsync(client, "SearchWidgets", 12, null, null, CancellationToken.None);

        Assert.Equal("https://example.test/widgets/search", handler.LastRequest!.RequestUri!.ToString());
        Assert.Equal("12", Assert.Single(handler.LastRequest.Headers["X-Request-ID"]));
    }

    [Fact]
    public async Task SearchWidgets_SerializesZeroDecimalWithoutScientificNotation()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "[]")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);

        await InvokeAsync(client, "SearchWidgets", 12, null, 0m, CancellationToken.None);

        Assert.Equal("https://example.test/widgets/search?score=0", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task SearchWidgets_SerializesNegativeDecimalUsingInvariantFormat()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "[]")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);

        await InvokeAsync(client, "SearchWidgets", 12, null, -12.75m, CancellationToken.None);

        Assert.Equal("https://example.test/widgets/search?score=-12.75", handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task SearchWidgets_SerializesCreatedAfterWithoutScore()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "[]")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var createdAfter = new DateTimeOffset(2024, 6, 7, 8, 9, 10, TimeSpan.Zero);

        await InvokeAsync(client, "SearchWidgets", 12, createdAfter, null, CancellationToken.None);

        var expectedCreatedAfter = Uri.EscapeDataString(createdAfter.ToString("O", System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(
            $"https://example.test/widgets/search?createdAfter={expectedCreatedAfter}",
            handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task SearchWidgets_SendsNoBodyOrContentType()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "[]")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);

        await InvokeAsync(client, "SearchWidgets", 12, null, null, CancellationToken.None);

        Assert.Null(handler.LastRequest!.Body);
        Assert.Null(handler.LastRequest.ContentType);
    }

    [Fact]
    public async Task CreateWidgetModel_SerializesObjectBodyAsJson()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "{\"id\":9,\"name\":\"created\"}")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var widgetModel = CreateWidgetModel("sample", 3);

        await InvokeAsync(client, "CreateWidgetModel", widgetModel, CancellationToken.None);

        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://example.test/widgets/model", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("application/json; charset=utf-8", handler.LastRequest.ContentType);
        Assert.Contains("\"name\":\"sample\"", handler.LastRequest.Body!, StringComparison.Ordinal);
        Assert.Contains("\"quantity\":3", handler.LastRequest.Body!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateWidgetModel_SendsNoCustomHeaders()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "{\"id\":9,\"name\":\"created\"}")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var widgetModel = CreateWidgetModel("sample", 3);

        await InvokeAsync(client, "CreateWidgetModel", widgetModel, CancellationToken.None);

        Assert.Empty(handler.LastRequest!.Headers);
    }

    [Fact]
    public async Task CreateWidgetModel_SerializesQuantityAsNullWhenNotProvided()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "{\"id\":9,\"name\":\"created\"}")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var widgetModel = CreateWidgetModel("sample");

        await InvokeAsync(client, "CreateWidgetModel", widgetModel, CancellationToken.None);

        Assert.Contains("\"name\":\"sample\"", handler.LastRequest!.Body!, StringComparison.Ordinal);
        Assert.Contains("\"quantity\":null", handler.LastRequest.Body!, StringComparison.Ordinal);
    }
}
