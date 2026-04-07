using Sellorio.Results.Json;
using Sellorio.Results.Messages;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Serialization;

namespace Sellorio.Results;

[JsonConverter(typeof(ResultJsonConverter))]
public class Result : IResult
{
    public bool WasSuccess { get; }

    public IReadOnlyList<ResultMessage> Messages { get; }

    internal Result(ImmutableArray<ResultMessage> messages)
    {
        WasSuccess = !messages.Any(x => x.Severity is ResultMessageSeverity.Critical or ResultMessageSeverity.Error or ResultMessageSeverity.NotFound);
        Messages = messages;
    }

    public static Result Success()
    {
        return new Result([]);
    }

    public static Result Create(IEnumerable<ResultMessage> messages)
    {
        messages ??= [];
        return new Result([.. messages]);
    }

    public static Result Create(params ResultMessage[] messages)
    {
        messages ??= [];
        return new Result([.. messages]);
    }

    public static Result operator |(Result left, Result right)
    {
        return new Result(Enumerable.Concat(left.Messages, right.Messages).ToImmutableArray());
    }

    public static implicit operator Result(ResultMessage message)
    {
        return new Result([message]);
    }
}
