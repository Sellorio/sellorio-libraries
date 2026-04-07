using Sellorio.Results.Json;
using Sellorio.Results.Messages;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json.Serialization;

namespace Sellorio.Results;

[JsonConverter(typeof(ResultJsonConverter))]
public class Result<TContext> : IResult
{
    public bool WasSuccess { get; }

    public IReadOnlyList<ResultMessage> Messages { get; }

    internal Result(ImmutableArray<ResultMessage> messages)
    {
        WasSuccess = !messages.Any(x => x.Severity is ResultMessageSeverity.Critical or ResultMessageSeverity.Error or ResultMessageSeverity.NotFound);
        Messages = messages;
    }

    public Result<TNewContext> Select<TNewContext>(Expression<Func<TContext, TNewContext>> pathToChild)
    {
        var relativePath = ExpressionToPathHelper.GetPath(pathToChild);

        var messages =
            Messages
                .Where(x => x.Path != null && x.Path.Take(relativePath.Length).SequenceEqual(relativePath))
                .Select(x => new ResultMessage(x.Text, x.Severity, x.Path!.Skip(relativePath.Length).ToImmutableArray()))
                .ToImmutableArray();

        return new Result<TNewContext>(messages);
    }

    public Result<TNewContext> SelectOut<TNewContext>(Expression<Func<TNewContext, TContext>> pathFromParent)
    {
        var relativePath = ExpressionToPathHelper.GetPath(pathFromParent);

        var messages =
            Messages
                .Select(x => new ResultMessage(x.Text, x.Severity, x.Path == null ? null : Enumerable.Concat(relativePath, x.Path).ToImmutableArray()))
                .ToImmutableArray();

        return new Result<TNewContext>(messages);
    }

    public Result<TContext> Exclude<TNewContext>(params Expression<Func<TContext, TNewContext>>[] pathToChildren)
    {
        var relativePaths = pathToChildren.Select(ExpressionToPathHelper.GetPath).ToList();

        var messages =
            Messages
                .Where(x => x.Path == null || !relativePaths.Any(y => x.Path.Take(y.Length).SequenceEqual(y)))
                .ToImmutableArray();

        return new Result<TContext>(messages);
    }

    public static Result<TContext> Create(params ResultMessage[] messages)
    {
        messages ??= [];
        return new Result<TContext>(messages.ToImmutableArray());
    }

    public static Result<TContext> operator |(Result<TContext> left, Result right)
    {
        return new Result<TContext>(Enumerable.Concat(left.Messages, right.Messages).ToImmutableArray());
    }

    public static Result<TContext> operator |(Result left, Result<TContext> right)
    {
        return new Result<TContext>(Enumerable.Concat(left.Messages, right.Messages).ToImmutableArray());
    }

    public static Result<TContext> operator |(Result<TContext> left, Result<TContext> right)
    {
        return new Result<TContext>(Enumerable.Concat(left.Messages, right.Messages).ToImmutableArray());
    }

    public static implicit operator Result<TContext>(ResultMessage message)
    {
        return new Result<TContext>([message]);
    }
}
