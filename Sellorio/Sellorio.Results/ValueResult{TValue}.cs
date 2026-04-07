using Sellorio.Results.Json;
using Sellorio.Results.Messages;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Serialization;

namespace Sellorio.Results;

[JsonConverter(typeof(ResultJsonConverter))]
public class ValueResult<TValue> : IResult
{
    private readonly TValue? _value;

    public bool WasSuccess { get; }

    public IReadOnlyList<ResultMessage> Messages { get; }
    public TValue Value => WasSuccess ? _value! : throw new InvalidOperationException("Cannot get result value for unsuccessful results.");

    private ValueResult(ImmutableArray<ResultMessage> messages, TValue? value)
    {
        WasSuccess = !messages.Any(x => x.Severity is ResultMessageSeverity.Critical or ResultMessageSeverity.Error or ResultMessageSeverity.NotFound);
        Messages = messages;
        _value = value;
    }

    public Result AsResult()
    {
        return new Result((ImmutableArray<ResultMessage>)Messages);
    }

    public static ValueResult<TValue> Success(TValue value, params ResultMessage[] messages)
    {
        return Success(value, (IEnumerable<ResultMessage>)messages);
    }

    public static ValueResult<TValue> Success(TValue value, IEnumerable<ResultMessage> messages)
    {
        messages ??= [];

        if (messages.Any(x => x.Severity is ResultMessageSeverity.Critical or ResultMessageSeverity.Error or ResultMessageSeverity.NotFound))
        {
            throw new ArgumentException("Cannot create a successful result with Critical or Error messages.", nameof(messages));
        }

        return new ValueResult<TValue>(messages.ToImmutableArray(), value);
    }

    public static ValueResult<TValue> Failure(params ResultMessage[] messages)
    {
        return Failure((IList<ResultMessage>)messages);
    }

    public static ValueResult<TValue> Failure(IEnumerable<ResultMessage> messages)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var asImmutableArray = messages.ToImmutableArray();

        if (!asImmutableArray.Any(x => x.Severity is ResultMessageSeverity.Critical or ResultMessageSeverity.Error or ResultMessageSeverity.NotFound))
        {
            throw new ArgumentException("Cannot create a failed result without any Critical or Error messages.", nameof(messages));
        }

        return new ValueResult<TValue>(asImmutableArray, default);
    }

    public static ValueResult<TValue> operator |(ValueResult<TValue> left, Result right)
    {
        return new ValueResult<TValue>(Enumerable.Concat(left.Messages, right.Messages).ToImmutableArray(), left.Value);
    }

    public static ValueResult<TValue> operator |(Result left, ValueResult<TValue> right)
    {
        return new ValueResult<TValue>(Enumerable.Concat(left.Messages, right.Messages).ToImmutableArray(), right.Value);
    }

    public static implicit operator ValueResult<TValue>(ResultMessage message)
    {
        return ValueResult<TValue>.Failure(message);
    }

    public static implicit operator ValueResult<TValue>(TValue value)
    {
        return ValueResult<TValue>.Success(value);
    }
}
