using System.Threading.Tasks;
using Sellorio.Results;
using Sellorio.Results.Messages;

namespace Sellorio.Blazor.Components;

public static class ExtensionsForResult
{
    public static async Task<Result> WithSuccessMessage(this Task<Result> resultTask, string message)
    {
        var result = await resultTask;

        if (result.WasSuccess && result.Messages.Count == 0)
        {
            return Result.Create(ResultMessage.Information(message));
        }

        return result;
    }

    public static async Task<Result<TContext>> WithSuccessMessage<TContext>(this Task<Result<TContext>> resultTask, string message)
    {
        var result = await resultTask;

        if (result.WasSuccess && result.Messages.Count == 0)
        {
            return Result<TContext>.Create(ResultMessage.Information(message));
        }

        return result;
    }

    public static async Task<ValueResult<TValue>> WithSuccessMessage<TValue>(this Task<ValueResult<TValue>> resultTask, string message)
    {
        var result = await resultTask;

        if (result.WasSuccess && result.Messages.Count == 0)
        {
            return ValueResult<TValue>.Success(result.Value, ResultMessage.Information(message));
        }

        return result;
    }

    public static async Task<ValueResult<TContext, TValue>> WithSuccessMessage<TContext, TValue>(this Task<ValueResult<TContext, TValue>> resultTask, string message)
    {
        var result = await resultTask;

        if (result.WasSuccess && result.Messages.Count == 0)
        {
            return ValueResult<TContext, TValue>.Success(result.Value, ResultMessage.Information(message));
        }

        return result;
    }
}
