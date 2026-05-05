using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Sellorio.Results;
using Sellorio.Results.Messages;

namespace Sellorio.Clients.Rest;

public static class ExtensionsForResult
{
    public static Task<Result> ToResult(this Task<HttpResponseMessage> responseMessageTask, JsonSerializerOptions? jsonSerializerOptions)
    {
        return ToResultAsync(
            responseMessageTask,
            (_, __) => Task.FromResult(Result.Success()),
            message => message,
            jsonSerializerOptions);
    }

    public static Task<Result> ToResult(this Task<HttpResponseMessage> responseMessageTask)
    {
        return ToResult(responseMessageTask, null);
    }

    public static Task<Result<TContext>> ToResult<TContext>(this Task<HttpResponseMessage> responseMessageTask, JsonSerializerOptions? jsonSerializerOptions)
    {
        return ToResultAsync(
            responseMessageTask,
            (_, __) => Task.FromResult(Result<TContext>.Create()),
            message => message,
            jsonSerializerOptions);
    }

    public static Task<Result<TContext>> ToResult<TContext>(this Task<HttpResponseMessage> responseMessageTask)
    {
        return ToResult<TContext>(responseMessageTask, null);
    }

    public static Task<ValueResult<TValue>> ToValueResult<TValue>(this Task<HttpResponseMessage> responseMessageTask, JsonSerializerOptions? jsonSerializerOptions)
    {
        return ToResultAsync(
            responseMessageTask,
            DeserializeValueResultAsync<TValue>,
            message => message,
            jsonSerializerOptions);
    }

    public static Task<ValueResult<TValue>> ToValueResult<TValue>(this Task<HttpResponseMessage> responseMessageTask)
    {
        return ToValueResult<TValue>(responseMessageTask, null);
    }

    public static Task<ValueResult<TContext, TValue>> ToValueResult<TContext, TValue>(this Task<HttpResponseMessage> responseMessageTask, JsonSerializerOptions? jsonSerializerOptions)
    {
        return ToResultAsync(
            responseMessageTask,
            DeserializeValueResultAsync<TContext, TValue>,
            message => message,
            jsonSerializerOptions);
    }

    public static Task<ValueResult<TValue>> ToValueResult<TValue>(this Task<HttpResponseMessage> responseMessageTask, Func<HttpResponseMessage, Task<TValue>> successFactory, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        return ToResultAsync(
            responseMessageTask,
            async (responseMessage, _) => ValueResult<TValue>.Success(await successFactory(responseMessage).ConfigureAwait(false)),
            message => message,
            jsonSerializerOptions);
    }

    public static Task<ValueResult<TContext, TValue>> ToValueResult<TContext, TValue>(this Task<HttpResponseMessage> responseMessageTask)
    {
        return ToValueResult<TContext, TValue>(responseMessageTask, null);
    }

    public static Task<ValueResult<TContext, TValue>> ToValueResult<TContext, TValue>(this Task<HttpResponseMessage> responseMessageTask, Func<HttpResponseMessage, Task<TValue>> successFactory, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        return ToResultAsync(
            responseMessageTask,
            async (responseMessage, _) => ValueResult<TContext, TValue>.Success(await successFactory(responseMessage).ConfigureAwait(false)),
            message => message,
            jsonSerializerOptions);
    }

    private static async Task<TResult> ToResultAsync<TResult>(
        Task<HttpResponseMessage> responseMessageTask,
        Func<HttpResponseMessage, JsonSerializerOptions, Task<TResult>> successFactory,
        Func<ResultMessage, TResult> messageToResult,
        JsonSerializerOptions? jsonSerializerOptions = null)
    {
        var responseMessage = await responseMessageTask.ConfigureAwait(false);
        var options = jsonSerializerOptions ?? Constants.DefaultJsonOptions;

        switch (responseMessage.StatusCode)
        {
            case System.Net.HttpStatusCode.Unauthorized:
                throw new UnauthorizedException();
            case System.Net.HttpStatusCode.ServiceUnavailable:
                return messageToResult.Invoke(ResultMessage.Error("The server is experiencing unexpected down time."));
            case System.Net.HttpStatusCode.Forbidden:
                return messageToResult.Invoke(ResultMessage.Error("You are not allowed to do this."));
            case System.Net.HttpStatusCode.Created:
            case System.Net.HttpStatusCode.NoContent:
            case System.Net.HttpStatusCode.OK:
                return await successFactory(responseMessage, options).ConfigureAwait(false);
            case System.Net.HttpStatusCode.BadRequest:
            case System.Net.HttpStatusCode.NotFound:
                var responseText = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (responseText.Length > 0 && responseText.StartsWith('{'))
                {
                    var result = JsonSerializer.Deserialize<TResult>(responseText, options);
                    if (result != null)
                    {
                        return result;
                    }
                }

                if (responseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return messageToResult.Invoke(ResultMessage.NotFound("Resource"));
                }

                goto default;
            case System.Net.HttpStatusCode.InternalServerError:
            default:
                return messageToResult.Invoke(ResultMessage.Error("An internal error has occured."));
        }
    }

    private static async Task<ValueResult<TValue>> DeserializeValueResultAsync<TValue>(HttpResponseMessage responseMessage, JsonSerializerOptions options)
    {
        if (responseMessage.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return ValueResult<TValue>.Success(GetDefaultValue<TValue>());
        }

        var value = await DeserializeBodyAsync<TValue>(responseMessage, options).ConfigureAwait(false);
        return ValueResult<TValue>.Success(value);
    }

    private static async Task<ValueResult<TContext, TValue>> DeserializeValueResultAsync<TContext, TValue>(HttpResponseMessage responseMessage, JsonSerializerOptions options)
    {
        if (responseMessage.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return ValueResult<TContext, TValue>.Success(GetDefaultValue<TValue>());
        }

        var value = await DeserializeBodyAsync<TValue>(responseMessage, options).ConfigureAwait(false);
        return ValueResult<TContext, TValue>.Success(value);
    }

    private static async Task<TValue> DeserializeBodyAsync<TValue>(HttpResponseMessage responseMessage, JsonSerializerOptions options)
    {
        var responseText = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(responseText))
        {
            return GetDefaultValue<TValue>();
        }

        var value = JsonSerializer.Deserialize<TValue>(responseText, options);

        if (value == null)
        {
            return GetDefaultValue<TValue>();
        }

        return value;
    }

    private static TValue GetDefaultValue<TValue>()
    {
        var type = typeof(TValue);

        if (type == typeof(string) || type.IsAbstract || type.IsInterface)
        {
            return default!;
        }

        if (!type.IsValueType && type.GetConstructor(Type.EmptyTypes) != null)
        {
            return (TValue)Activator.CreateInstance(type)!;
        }

        return default!;
    }
}
