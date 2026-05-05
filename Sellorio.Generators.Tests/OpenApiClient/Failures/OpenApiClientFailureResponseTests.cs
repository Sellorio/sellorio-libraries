using Sellorio.Clients.Rest;
using Sellorio.Generators.Tests.OpenApiClient.Support;
using Sellorio.Results;
using Sellorio.Results.Messages;
using static Sellorio.Generators.Tests.OpenApiClient.Support.OpenApiClientTestSupport;

namespace Sellorio.Generators.Tests.OpenApiClient.Failures;

public sealed class OpenApiClientFailureResponseTests
{
    [Fact]
    public async Task GetReport_ReturnsErrorMessageForInternalServerError()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreatePlainResponse(HttpStatusCode.InternalServerError)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedResponseClient", handler);

        var response = (ValueResult<int>)(await InvokeAsync(client, "GetReport", 42, CancellationToken.None))!;

        Assert.False(response.WasSuccess);
        Assert.Contains(response.Messages, message => message.Text == "An internal error has occured.");
        Assert.All(response.Messages, message => Assert.Equal(ResultMessageSeverity.Error, message.Severity));
    }

    [Fact]
    public async Task GetReport_ReturnsErrorMessageForUnhandledStatusCode()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreatePlainResponse(HttpStatusCode.BadGateway)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedResponseClient", handler);

        var response = (ValueResult<int>)(await InvokeAsync(client, "GetReport", 42, CancellationToken.None))!;

        Assert.False(response.WasSuccess);
        var message = Assert.Single(response.Messages);
        Assert.Equal(ResultMessageSeverity.Error, message.Severity);
        Assert.Equal("An internal error has occured.", message.Text);
    }

    [Fact]
    public async Task GetWidget_ReturnsNotFoundSeverityFor404WithoutPayload()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreatePlainResponse(HttpStatusCode.NotFound)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var widgetId = Guid.Parse("58db2ebd-0ca5-4edf-807f-238339fd6141");

        dynamic response = (await InvokeAsync(client, "GetWidget", widgetId, null, null, CancellationToken.None))!;

        Assert.False((bool)response.WasSuccess);
        Assert.Equal("ValueResult`1", response.GetType().Name);
        Assert.Equal("GetWidgetResponse", response.GetType().GenericTypeArguments[0].Name);

        var message = Assert.Single((IEnumerable<ResultMessage>)response.Messages);
        Assert.Equal(ResultMessageSeverity.NotFound, message.Severity);
        Assert.Equal("Resource not found.", message.Text);
    }

    [Fact]
    public async Task CreateWidget_ReturnsTypedResultWithIndexedValidationPathForBadRequest()
    {
        using var handler = new CapturingHandler((_, _) =>
            Task.FromResult(CreateJsonResponse(HttpStatusCode.BadRequest, """
            {
              "m": [
                {
                  "p": [
                    {
                      "v": 1,
                      "t": 1
                    }
                  ],
                  "s": 1,
                  "t": "Widget name is required."
                }
              ],
              "v": null
            }
            """)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);

        dynamic response = (await InvokeAsync(client, "CreateWidget", WidgetPayload, CancellationToken.None))!;

        Assert.False((bool)response.WasSuccess);
        Assert.Equal("Result`1", response.GetType().Name);
        Assert.Equal("IReadOnlyList`1", response.GetType().GenericTypeArguments[0].Name);

        var message = Assert.Single((IEnumerable<ResultMessage>)response.Messages);
        Assert.Equal("Widget name is required.", message.Text);
        Assert.Equal(ResultMessageSeverity.Error, message.Severity);
        Assert.NotNull(message.Path);
        var path = message.Path!.ToArray();
        Assert.Single(path);
        Assert.Equal(ResultMessagePathItemType.Indexer, path[0].Type);
        Assert.Equal(1, path[0].Value);
    }

    [Fact]
    public async Task CreateWidgetModel_ReturnsTypedValueResultWithPropertyValidationPathForBadRequest()
    {
        using var handler = new CapturingHandler((_, _) =>
            Task.FromResult(CreateJsonResponse(HttpStatusCode.BadRequest, """
            {
              "m": [
                {
                  "p": [
                    {
                      "v": "Name",
                      "t": 0
                    }
                  ],
                  "s": 1,
                  "t": "Name is required."
                }
              ],
              "v": null
            }
            """)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var widgetModelType = typeof(IGeneratedBasicClient).Assembly.GetType("Sellorio.Generators.Tests.OpenApiClient.WidgetModel")!;
        var widgetModel = Activator.CreateInstance(widgetModelType)!;
        widgetModelType.GetProperty("Quantity")!.SetValue(widgetModel, 3);

        dynamic response = (await InvokeAsync(client, "CreateWidgetModel", widgetModel, CancellationToken.None))!;

        Assert.False((bool)response.WasSuccess);
        Assert.Equal("ValueResult`2", response.GetType().Name);
        Assert.Equal("WidgetModel", response.GetType().GenericTypeArguments[0].Name);
        Assert.Equal("CreateWidgetModelResponse", response.GetType().GenericTypeArguments[1].Name);

        var message = Assert.Single((IEnumerable<ResultMessage>)response.Messages);
        Assert.Equal("Name is required.", message.Text);
        Assert.Equal(ResultMessageSeverity.Error, message.Severity);
        Assert.NotNull(message.Path);
        var path = message.Path!.ToArray();
        Assert.Single(path);
        Assert.Equal(ResultMessagePathItemType.Property, path[0].Type);
        Assert.Equal("Name", path[0].Value);
    }

    [Fact]
    public async Task CreateWidgetModel_ReturnsStructuredBadRequestPayloadWithMultipleMessages()
    {
        using var handler = new CapturingHandler((_, _) =>
            Task.FromResult(CreateJsonResponse(HttpStatusCode.BadRequest, """
            {
              "m": [
                {
                  "p": [
                    {
                      "v": "Name",
                      "t": 0
                    }
                  ],
                  "s": 1,
                  "t": "Name is required."
                },
                {
                  "p": [
                    {
                      "v": "Quantity",
                      "t": 0
                    }
                  ],
                  "s": 1,
                  "t": "Quantity must be positive."
                }
              ],
              "v": null
            }
            """)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var widgetModel = CreateWidgetModel("sample", -1);

        dynamic response = (await InvokeAsync(client, "CreateWidgetModel", widgetModel, CancellationToken.None))!;

        Assert.False((bool)response.WasSuccess);
        var messages = ((IEnumerable<ResultMessage>)response.Messages).ToArray();
        Assert.Equal(2, messages.Length);
        Assert.Contains(messages, message => message.Text == "Name is required.");
        Assert.Contains(messages, message => message.Text == "Quantity must be positive.");
    }

    [Fact]
    public async Task GetWidget_ReturnsStructuredNotFoundPayloadWhenProvided()
    {
        using var handler = new CapturingHandler((_, _) =>
            Task.FromResult(CreateJsonResponse(HttpStatusCode.NotFound, """
            {
              "m": [
                {
                  "p": [],
                  "s": 2,
                  "t": "Widget not found."
                }
              ],
              "v": null
            }
            """)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var widgetId = Guid.Parse("e577b4bc-4d73-4c8d-a490-3fbb6cac6af0");

        dynamic response = (await InvokeAsync(client, "GetWidget", widgetId, null, null, CancellationToken.None))!;

        Assert.False((bool)response.WasSuccess);
        var message = Assert.Single((IEnumerable<ResultMessage>)response.Messages);
        Assert.Equal(ResultMessageSeverity.NotFound, message.Severity);
        Assert.Equal("Widget not found.", message.Text);
    }

    [Fact]
    public async Task GetWidget_ReturnsStructuredNotFoundPayloadWithoutLosingValueTypeShape()
    {
        using var handler = new CapturingHandler((_, _) =>
            Task.FromResult(CreateJsonResponse(HttpStatusCode.NotFound, """
            {
              "m": [
                {
                  "p": [],
                  "s": 2,
                  "t": "Widget not found."
                }
              ],
              "v": null
            }
            """)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var widgetId = Guid.Parse("e577b4bc-4d73-4c8d-a490-3fbb6cac6af1");

        dynamic response = (await InvokeAsync(client, "GetWidget", widgetId, null, null, CancellationToken.None))!;

        Assert.Equal("ValueResult`1", response.GetType().Name);
        Assert.Equal("GetWidgetResponse", response.GetType().GenericTypeArguments[0].Name);
    }

    [Fact]
    public async Task GetReport_ThrowsJsonExceptionForStructuredBadRequestPayloadWithNullPrimitiveValue()
    {
        using var handler = new CapturingHandler((_, _) =>
            Task.FromResult(CreateJsonResponse(HttpStatusCode.BadRequest, """
            {
              "m": [
                {
                  "p": [],
                  "s": 1,
                  "t": "Report id is invalid."
                }
              ],
              "v": null
            }
            """)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedResponseClient", handler);

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(
            async () => await InvokeAsync(client, "GetReport", 42, CancellationToken.None));
    }

    [Fact]
    public async Task GetDefaultReport_ReturnsStructuredBadRequestPayloadWhenProvided()
    {
        using var handler = new CapturingHandler((_, _) =>
            Task.FromResult(CreateJsonResponse(HttpStatusCode.BadRequest, """
            {
              "m": [
                {
                  "p": [],
                  "s": 1,
                  "t": "Default report is invalid."
                }
              ],
              "v": null
            }
            """)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedResponseClient", handler);

        var response = (ValueResult<string>)(await InvokeAsync(client, "GetDefaultReport", CancellationToken.None))!;

        Assert.False(response.WasSuccess);
        var message = Assert.Single(response.Messages);
        Assert.Equal(ResultMessageSeverity.Error, message.Severity);
        Assert.Equal("Default report is invalid.", message.Text);
    }

    [Fact]
    public async Task GetWidget_ReturnsStructuredNotFoundPayloadWithPathWhenProvided()
    {
        using var handler = new CapturingHandler((_, _) =>
            Task.FromResult(CreateJsonResponse(HttpStatusCode.NotFound, """
            {
              "m": [
                {
                  "p": [
                    {
                      "v": "Name",
                      "t": 0
                    }
                  ],
                  "s": 2,
                  "t": "Widget name not found."
                }
              ],
              "v": null
            }
            """)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var widgetId = Guid.Parse("30e3a1d4-f509-4e6f-a723-0c4336f5ef7e");

        dynamic response = (await InvokeAsync(client, "GetWidget", widgetId, null, null, CancellationToken.None))!;

        Assert.False((bool)response.WasSuccess);
        var message = Assert.Single((IEnumerable<ResultMessage>)response.Messages);
        Assert.Equal(ResultMessageSeverity.NotFound, message.Severity);
        Assert.Equal("Widget name not found.", message.Text);
        Assert.NotNull(message.Path);
        var path = message.Path!.ToArray();
        Assert.Single(path);
        Assert.Equal(ResultMessagePathItemType.Property, path[0].Type);
        Assert.Equal("Name", path[0].Value);
    }

    [Fact]
    public async Task GetDefaultReport_ReturnsErrorForForbidden()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreatePlainResponse(HttpStatusCode.Forbidden)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedResponseClient", handler);

        var response = (ValueResult<string>)(await InvokeAsync(client, "GetDefaultReport", CancellationToken.None))!;

        Assert.False(response.WasSuccess);
        var message = Assert.Single(response.Messages);
        Assert.Equal(ResultMessageSeverity.Error, message.Severity);
        Assert.Equal("You are not allowed to do this.", message.Text);
    }

    [Fact]
    public async Task GetDefaultReport_ReturnsErrorForServiceUnavailable()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreatePlainResponse(HttpStatusCode.ServiceUnavailable)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedResponseClient", handler);

        var response = (ValueResult<string>)(await InvokeAsync(client, "GetDefaultReport", CancellationToken.None))!;

        Assert.False(response.WasSuccess);
        var message = Assert.Single(response.Messages);
        Assert.Equal(ResultMessageSeverity.Error, message.Severity);
        Assert.Equal("The server is experiencing unexpected down time.", message.Text);
    }

    [Fact]
    public async Task GetAdminReport_ReturnsErrorForForbidden()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreatePlainResponse(HttpStatusCode.Forbidden)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);

        dynamic response = (await InvokeAsync(client, "GetAdminReport", CancellationToken.None))!;

        Assert.False((bool)response.WasSuccess);
        var message = Assert.Single((IEnumerable<ResultMessage>)response.Messages);
        Assert.Equal(ResultMessageSeverity.Error, message.Severity);
        Assert.Equal("You are not allowed to do this.", message.Text);
    }

    [Fact]
    public async Task SearchWidgets_ReturnsErrorForServiceUnavailable()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreatePlainResponse(HttpStatusCode.ServiceUnavailable)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);

        var response = (ValueResult<IReadOnlyList<string>>)(await InvokeAsync(client, "SearchWidgets", 5, null, null, CancellationToken.None))!;

        Assert.False(response.WasSuccess);
        var message = Assert.Single(response.Messages);
        Assert.Equal(ResultMessageSeverity.Error, message.Severity);
        Assert.Equal("The server is experiencing unexpected down time.", message.Text);
    }

    [Fact]
    public async Task GetDefaultReport_ThrowsUnauthorizedExceptionFor401()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreatePlainResponse(HttpStatusCode.Unauthorized)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedResponseClient", handler);

        await Assert.ThrowsAsync<UnauthorizedException>(
            async () => await InvokeAsync(client, "GetDefaultReport", CancellationToken.None));
    }

    [Fact]
    public async Task GetWidget_ThrowsUnauthorizedExceptionFor401()
    {
        using var handler = new CapturingHandler((_, _) => Task.FromResult(CreatePlainResponse(HttpStatusCode.Unauthorized)));
        var client = CreateClient("Sellorio.Generators.Tests.OpenApiClient.GeneratedBasicClient", handler);
        var widgetId = Guid.Parse("4582412f-f6c1-4d38-91d4-c6b6ba11dc4e");

        await Assert.ThrowsAsync<UnauthorizedException>(
            async () => await InvokeAsync(client, "GetWidget", widgetId, null, null, CancellationToken.None));
    }
}
