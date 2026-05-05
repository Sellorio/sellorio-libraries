using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Sellorio.Generators.Tests.OpenApiClient;

public sealed class OpenApiClientGenerationTests
{
    private static readonly IReadOnlyList<string> _widgetPayload = ["first", "second"];

    [Fact]
    public void GeneratedInterface_ExposesExpectedOperations()
    {
        var basicInterface = typeof(IGeneratedBasicClient);
        var filteredInterface = typeof(IGeneratedFilteredClient);

        Assert.Equal(
            ["CreateWidget", "GetAdminReport", "GetWidget", "SearchWidgets"],
            basicInterface
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Select(method => method.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray());

        Assert.Equal(
            ["CreateWidget", "GetWidget", "SearchWidgets"],
            filteredInterface
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Select(method => method.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray());
    }

    [Fact]
    public void GeneratedInterface_UsesExpectedParameterTypesAndOptionalDefaults()
    {
        var assembly = typeof(IGeneratedBasicClient).Assembly;
        var getWidget = typeof(IGeneratedBasicClient).GetMethod("GetWidget");
        Assert.NotNull(getWidget);

        var getWidgetParameters = getWidget.GetParameters();
        Assert.Collection(
            getWidgetParameters,
            parameter =>
            {
                Assert.Equal("widgetId", parameter.Name);
                Assert.Equal(typeof(Guid), parameter.ParameterType);
                Assert.False(parameter.IsOptional);
            },
            parameter =>
            {
                Assert.Equal("includeDetails", parameter.Name);
                Assert.Equal(typeof(bool?), parameter.ParameterType);
                Assert.True(parameter.IsOptional);
                Assert.Null(parameter.DefaultValue);
            },
            parameter =>
            {
                Assert.Equal("xCorrelationId", parameter.Name);
                Assert.Equal(typeof(string), parameter.ParameterType);
                Assert.True(parameter.IsOptional);
                Assert.Null(parameter.DefaultValue);
            },
            parameter =>
            {
                Assert.Equal("cancellationToken", parameter.Name);
                Assert.Equal(typeof(CancellationToken), parameter.ParameterType);
                Assert.True(parameter.IsOptional);
            });

        var searchWidgets = typeof(IGeneratedBasicClient).GetMethod("SearchWidgets");
        Assert.NotNull(searchWidgets);

        Assert.Collection(
            searchWidgets.GetParameters(),
            parameter =>
            {
                Assert.Equal("xRequestId", parameter.Name);
                Assert.Equal(typeof(int), parameter.ParameterType);
                Assert.False(parameter.IsOptional);
            },
            parameter =>
            {
                Assert.Equal("createdAfter", parameter.Name);
                Assert.Equal(typeof(DateTimeOffset?), parameter.ParameterType);
                Assert.True(parameter.IsOptional);
                Assert.Null(parameter.DefaultValue);
            },
            parameter =>
            {
                Assert.Equal("score", parameter.Name);
                Assert.Equal(typeof(decimal?), parameter.ParameterType);
                Assert.True(parameter.IsOptional);
                Assert.Null(parameter.DefaultValue);
            },
            parameter =>
            {
                Assert.Equal("cancellationToken", parameter.Name);
                Assert.Equal(typeof(CancellationToken), parameter.ParameterType);
                Assert.True(parameter.IsOptional);
            });

        var createWidget = typeof(IGeneratedBasicClient).GetMethod("CreateWidget");
        Assert.NotNull(createWidget);
        Assert.Collection(
            createWidget.GetParameters(),
            parameter =>
            {
                Assert.Equal("widgetPayload", parameter.Name);
                Assert.Equal(typeof(IReadOnlyList<string>), parameter.ParameterType);
                Assert.False(parameter.IsOptional);
            },
            parameter =>
            {
                Assert.Equal("cancellationToken", parameter.Name);
                Assert.Equal(typeof(CancellationToken), parameter.ParameterType);
                Assert.True(parameter.IsOptional);
            });

        Assert.NotNull(assembly.GetType("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient"));
        Assert.NotNull(assembly.GetType("Sellorio.Generators.Tests.OpenApiClient.GeneratedResponseClient"));
        Assert.Null(assembly.GetType("Sellorio.Generators.Tests.OpenApiClient.GeneratedFilteredClient"));
    }

    [Fact]
    public async Task GeneratedImplementation_SendsExpectedRequestAndParsesResponse()
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

        var response = (JsonElement)(await InvokeAsync(client, "GetWidget", widgetId, true, "corr-123", CancellationToken.None))!;

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal($"https://example.test/widgets/{widgetId:D}?includeDetails=True", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("corr-123", Assert.Single(handler.LastRequest.Headers["X-Correlation-ID"]));
        Assert.Equal(JsonValueKind.Object, response.ValueKind);
        Assert.Equal("Widget", response.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GeneratedImplementation_OmitsUnsetOptionalQueryAndHeaderParameters()
    {
        using var handler = new CapturingHandler((_, _) =>
            Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "{}")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var widgetId = Guid.Parse("c3f8b6f3-3b43-4638-a5cb-5c12911bb87a");

        await InvokeAsync(client, "GetWidget", widgetId, null, null, CancellationToken.None);

        Assert.Equal($"https://example.test/widgets/{widgetId:D}", handler.LastRequest!.RequestUri!.ToString());
        Assert.False(handler.LastRequest.Headers.ContainsKey("X-Correlation-ID"));
    }

    [Fact]
    public async Task GeneratedImplementation_SerializesBodyAndOptionalParameters()
    {
        using var response1 = CreatePlainResponse(HttpStatusCode.NoContent);
        using var response2 = CreateJsonResponse(HttpStatusCode.OK, "[]");
        var responses = new Queue<HttpResponseMessage>([
            response1,
            response2]);
        using var handler = new CapturingHandler((request, cancellationToken) =>
        {
            _ = request;
            _ = cancellationToken;
            return Task.FromResult(responses.Dequeue());
        });
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);

        await InvokeAsync(client, "CreateWidget", _widgetPayload, CancellationToken.None);

        Assert.Equal(HttpMethod.Post, handler.Requests[0].Method);
        Assert.Equal("https://example.test/widgets", handler.Requests[0].RequestUri!.ToString());
        Assert.Equal("application/json; charset=utf-8", handler.Requests[0].ContentType);
        Assert.Equal("[\"first\",\"second\"]", handler.Requests[0].Body);

        var createdAfter = DateTimeOffset.Parse("2024-05-01T11:22:33.4444444+00:00", null, System.Globalization.DateTimeStyles.RoundtripKind);
        await InvokeAsync(client, "SearchWidgets", 12, createdAfter, 12.5m, CancellationToken.None);

        Assert.Equal(HttpMethod.Get, handler.Requests[1].Method);
        Assert.Equal("12", Assert.Single(handler.Requests[1].Headers["X-Request-ID"]));
        Assert.Equal(
            $"https://example.test/widgets/search?createdAfter={Uri.EscapeDataString(createdAfter.ToString("O", System.Globalization.CultureInfo.InvariantCulture))}&score=12.5",
            handler.Requests[1].RequestUri!.ToString());
    }

    [Fact]
    public async Task GeneratedImplementation_ParsesTypedNullableQueryParameterUsingInvariantFormat()
    {
        using var handler = new CapturingHandler((_, _) =>
            Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, "[]")));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var createdAfter = new DateTimeOffset(2025, 1, 2, 3, 4, 5, TimeSpan.Zero).AddTicks(6789);

        await InvokeAsync(client, "SearchWidgets", 99, createdAfter, 1234.5m, CancellationToken.None);

        Assert.Contains(
            "createdAfter=2025-01-02T03%3A04%3A05.0006789%2B00%3A00",
            handler.LastRequest!.RequestUri!.Query,
            StringComparison.Ordinal);
        Assert.Contains("score=1234.5", handler.LastRequest.RequestUri.Query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GeneratedImplementation_ReturnsDefaultJsonElementForEmptyJsonBody()
    {
        using var handler = new CapturingHandler((_, _) =>
            Task.FromResult(CreateJsonResponse(HttpStatusCode.OK, string.Empty)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedResponseClient", handler);

        var response = (JsonElement)(await InvokeAsync(client, "GetEmptyReport", CancellationToken.None))!;

        Assert.Equal(JsonValueKind.Undefined, response.ValueKind);
    }

    [Fact]
    public async Task GeneratedImplementation_ReturnsExpectedTypedAndNoContentResponses()
    {
        using var response1 = CreateJsonResponse(HttpStatusCode.OK, "123");
        using var response2 = CreateJsonResponse(HttpStatusCode.OK, "\"fallback\"");
        using var response3 = CreatePlainResponse(HttpStatusCode.NoContent);
        var responses = new Queue<HttpResponseMessage>([
            response1,
            response2,
            response3]);
        using var handler = new CapturingHandler((_, _) => Task.FromResult(responses.Dequeue()));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedResponseClient", handler);

        var typedResponse = (int)(await InvokeAsync(client, "GetReport", 42, CancellationToken.None))!;
        var defaultResponse = (string)(await InvokeAsync(client, "GetDefaultReport", CancellationToken.None))!;
        await InvokeAsync(client, "GetPlainReport", CancellationToken.None);

        Assert.Equal(123, typedResponse);
        Assert.Equal("fallback", defaultResponse);
        Assert.Equal(HttpMethod.Get, handler.Requests[2].Method);
        Assert.Equal("https://example.test/reports/plain", handler.Requests[2].RequestUri!.ToString());
    }

    private static object CreateClient(string typeName, CapturingHandler handler)
    {
        var clientType = typeof(IGeneratedBasicClient).Assembly.GetType(typeName);
        Assert.NotNull(clientType);
        return Activator.CreateInstance(clientType!, new HttpClient(handler) { BaseAddress = new Uri("https://example.test") })!;
    }

    private static async Task<object?> InvokeAsync(object instance, string methodName, params object?[] arguments)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);

        var task = method.Invoke(instance, arguments) as Task;
        Assert.NotNull(task);

        await task;

        var resultProperty = task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
        return resultProperty?.GetValue(task);
    }

    private static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string body)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }

    private static HttpResponseMessage CreatePlainResponse(HttpStatusCode statusCode)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(string.Empty),
        };
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        private readonly List<CapturedRequest> _requests = [];

        public IReadOnlyList<CapturedRequest> Requests => _requests;

        public CapturedRequest? LastRequest => _requests.Count == 0 ? null : _requests[^1];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content == null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            var headers = request.Headers.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);

            _requests.Add(new CapturedRequest(
                request.Method,
                request.RequestUri,
                headers,
                body,
                request.Content?.Headers.ContentType?.ToString()));

            return await handler(request, cancellationToken);
        }
    }

    private sealed class CapturedRequest(
        HttpMethod method,
        Uri? requestUri,
        IReadOnlyDictionary<string, string[]> headers,
        string? body,
        string? contentType)
    {
        public HttpMethod Method { get; } = method;

        public Uri? RequestUri { get; } = requestUri;

        public IReadOnlyDictionary<string, string[]> Headers { get; } = headers;

        public string? Body { get; } = body;

        public string? ContentType { get; } = contentType;
    }
}
