using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Sellorio.Results;
using Sellorio.Results.Messages;

namespace Sellorio.Clients.Rest;

public static class ExtensionsForResult
{
    public static Task<Result> ToResult(this Task<HttpResponseMessage> responseMessageTask)
    {
        return ToResult<Result>(responseMessageTask, error => ResultMessage.Error(error));
    }

    public static Task<Result<TContext>> ToResult<TContext>(this Task<HttpResponseMessage> responseMessageTask)
    {
        return ToResult<Result<TContext>>(responseMessageTask, error => ResultMessage.Error(error));
    }

    public static Task<ValueResult<TValue>> ToValueResult<TValue>(this Task<HttpResponseMessage> responseMessageTask)
    {
        return ToResult<ValueResult<TValue>>(responseMessageTask, error => ResultMessage.Error(error));
    }

    public static Task<ValueResult<TContext, TValue>> ToValueResult<TContext, TValue>(this Task<HttpResponseMessage> responseMessageTask)
    {
        return ToResult<ValueResult<TContext, TValue>>(responseMessageTask, error => ResultMessage.Error(error));
    }

    private static async Task<TResult> ToResult<TResult>(
        Task<HttpResponseMessage> responseMessageTask,
        Func<string, TResult> errorToResult,
        JsonSerializerOptions? jsonSerializerOptions = null)
    {
        var responseMessage = await responseMessageTask;

        switch (responseMessage.StatusCode)
        {
            case System.Net.HttpStatusCode.Unauthorized:
                throw new UnauthorizedException();
            case System.Net.HttpStatusCode.ServiceUnavailable:
                return errorToResult.Invoke("The server is experiencing unexpected down time.");
            case System.Net.HttpStatusCode.Forbidden:
                return errorToResult.Invoke("You are not allowed to do this.");
            case System.Net.HttpStatusCode.BadRequest:
            case System.Net.HttpStatusCode.Created:
            case System.Net.HttpStatusCode.NoContent:
            case System.Net.HttpStatusCode.OK:
                var responseText = await responseMessage.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResult>(responseText, jsonSerializerOptions ?? Constants.DefaultJsonOptions)!;
            case System.Net.HttpStatusCode.NotFound:
                responseText = await responseMessage.Content.ReadAsStringAsync();

                if (responseText.Length > 0 && responseText.StartsWith('{'))
                {
                    return JsonSerializer.Deserialize<TResult>(responseText, jsonSerializerOptions ?? Constants.DefaultJsonOptions)!;
                }
                else
                {
                    goto default;
                }
            case System.Net.HttpStatusCode.InternalServerError:
            default:
                return errorToResult.Invoke("An internal error has occured.");
            }
    }
}
