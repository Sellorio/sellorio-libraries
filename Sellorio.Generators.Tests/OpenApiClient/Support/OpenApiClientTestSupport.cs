using System.Reflection;
using System.Text;

namespace Sellorio.Generators.Tests.OpenApiClient.Support;

internal static class OpenApiClientTestSupport
{
    public static IReadOnlyList<string> WidgetPayload { get; } = ["first", "second"];

    public static Type GetGeneratedType(string fullTypeName)
    {
        var type = typeof(IGeneratedBasicClient).Assembly.GetType(fullTypeName);
        Assert.NotNull(type);
        return type;
    }

    public static object CreateWidgetModel(string? name = null, int? quantity = null)
    {
        var type = GetGeneratedType("Sellorio.Generators.Tests.OpenApiClient.WidgetModel");
        var instance = Activator.CreateInstance(type);
        Assert.NotNull(instance);

        if (name != null)
        {
            type.GetProperty("Name")!.SetValue(instance, name);
        }

        if (quantity.HasValue)
        {
            type.GetProperty("Quantity")!.SetValue(instance, quantity.Value);
        }

        return instance!;
    }

    public static object CreateClient(string typeName, CapturingHandler handler)
    {
        var clientType = GetGeneratedType(typeName);
        return Activator.CreateInstance(clientType!, new HttpClient(handler) { BaseAddress = new Uri("https://example.test") })!;
    }

    public static async Task<object?> InvokeAsync(object instance, string methodName, params object?[] arguments)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);

        var task = method.Invoke(instance, arguments) as Task;
        Assert.NotNull(task);

        await task.ConfigureAwait(false);

        var resultProperty = task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
        return resultProperty?.GetValue(task);
    }

    public static HttpResponseMessage CreateJsonResponse(HttpStatusCode statusCode, string body)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
    }

    public static HttpResponseMessage CreatePlainResponse(HttpStatusCode statusCode)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(string.Empty),
        };
    }
}

internal sealed class CapturingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
{
    private readonly List<CapturedRequest> _requests = [];

    public IReadOnlyList<CapturedRequest> Requests => _requests;

    public CapturedRequest? LastRequest => _requests.Count == 0 ? null : _requests[^1];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content == null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
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

        return await handler(request, cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class CapturedRequest(
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
