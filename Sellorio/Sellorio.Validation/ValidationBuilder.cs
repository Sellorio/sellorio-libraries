using Sellorio.Results;
using Sellorio.Results.Messages;
using Sellorio.Validation.Validators;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

#pragma warning disable CS0618 // Type or member is obsolete (IValidator interfaces that shouldn't be used directly)

namespace Sellorio.Validation;

internal class ValidationBuilder<TObject>(TObject target, IServiceProvider serviceProvider) : IValidationBuilder<TObject>
{
    internal List<ResultMessage> Messages { get; } = [];

    public bool IsValid => !Messages.Any(x => x.Severity is ResultMessageSeverity.Critical or ResultMessageSeverity.Error);

    public TObject Target => target;

    public IValidationBuilder<TObject> AddMessage(string message, ResultMessageSeverity severity = ResultMessageSeverity.Error)
    {
        Messages.Add(CreateMessage(message, severity));

        if (severity == ResultMessageSeverity.Critical)
        {
            throw new FastFailException();
        }

        return this;
    }

    public IValidationBuilder<TObject> AddMessage<TPathValue>(Expression<Func<TObject, TPathValue>> path, string message, ResultMessageSeverity severity = ResultMessageSeverity.Error)
    {
        Messages.Add(CreateMessage(path, message, severity));

        if (severity == ResultMessageSeverity.Critical)
        {
            throw new FastFailException();
        }

        return this;
    }

    public IValidationBuilder<TObject> For<TNewObject>(Expression<Func<TObject, TNewObject>> path, Action<IValidationBuilder<TNewObject>> validate)
    {
        var messagePath = ExpressionToPathHelper.GetPath(path);
        var childGetter = path.Compile();
        TNewObject? child = default;

        try
        {
            child = childGetter.Invoke(Target);
        }
        catch (NullReferenceException)
        {
            return this;
        }

        if (child != null)
        {
            var childValidationBuilder = new ValidationBuilder<TNewObject>(child, serviceProvider);

            try
            {
                validate.Invoke(childValidationBuilder);
            }
            finally
            {
                Messages.AddRange(
                    childValidationBuilder.Messages
                        .Select(x =>
                            new ResultMessage(x.Text, x.Severity, x.Path == null ? messagePath : Enumerable.Concat(messagePath, x.Path).ToImmutableArray())));
            }
        }

        return this;
    }

    public async Task ForAsync<TNewObject>(Expression<Func<TObject, TNewObject>> path, Func<IValidationBuilder<TNewObject>, Task> validate)
    {
        var messagePath = ExpressionToPathHelper.GetPath(path);
        var childGetter = path.Compile();
        TNewObject? child = default;

        try
        {
            child = childGetter.Invoke(Target);
        }
        catch (NullReferenceException)
        {
            return;
        }

        if (child != null)
        {
            var childValidationBuilder = new ValidationBuilder<TNewObject>(child, serviceProvider);

            try
            {
                await validate.Invoke(childValidationBuilder);
            }
            finally
            {
                Messages.AddRange(
                    childValidationBuilder.Messages
                        .Select(x =>
                            new ResultMessage(x.Text, x.Severity, x.Path == null ? messagePath : Enumerable.Concat(messagePath, x.Path).ToImmutableArray())));
            }
        }
    }

    public IValidationBuilder<TObject> ForEach<TNewObject>(Expression<Func<TObject, IEnumerable<TNewObject>>> path, Action<IValidationBuilder<TNewObject>> validate)
    {
        var messagePath = ExpressionToPathHelper.GetPath(path);
        var childGetter = path.Compile();
        IEnumerable<TNewObject>? child = default;

        try
        {
            child = childGetter.Invoke(Target);
        }
        catch (NullReferenceException)
        {
            return this;
        }

        if (child != null)
        {
            foreach (var item in child)
            {
                var childValidationBuilder = new ValidationBuilder<TNewObject>(item, serviceProvider);

                try
                {
                    validate.Invoke(childValidationBuilder);
                }
                finally
                {
                    Messages.AddRange(
                        childValidationBuilder.Messages
                            .Select(x =>
                                new ResultMessage(x.Text, x.Severity, x.Path == null ? messagePath : Enumerable.Concat(messagePath, x.Path).ToImmutableArray())));
                }
            }
        }

        return this;
    }

    public async Task ForEachAsync<TNewObject>(Expression<Func<TObject, IEnumerable<TNewObject>>> path, Func<IValidationBuilder<TNewObject>, Task> validate)
    {
        var messagePath = ExpressionToPathHelper.GetPath(path);
        var childGetter = path.Compile();
        IEnumerable<TNewObject>? child = default;

        try
        {
            child = childGetter.Invoke(Target);
        }
        catch (NullReferenceException)
        {
            return;
        }

        if (child != null)
        {
            foreach (var item in child)
            {
                var childValidationBuilder = new ValidationBuilder<TNewObject>(item, serviceProvider);

                try
                {
                    await validate.Invoke(childValidationBuilder);
                }
                finally
                {
                    var result = Result<TNewObject>.Create(childValidationBuilder.Messages.ToArray());

                    Messages.AddRange(
                        childValidationBuilder.Messages
                            .Select(x =>
                                new ResultMessage(x.Text, x.Severity, x.Path == null ? messagePath : Enumerable.Concat(messagePath, x.Path).ToImmutableArray())));
                }
            }
        }

        return;
    }

    public async Task UseValidator<TValidator>() where TValidator : IValidatorWithoutContext<TObject>
    {
        var validator =
            (TValidator?)serviceProvider.GetService(typeof(TValidator))
                ?? throw new InvalidOperationException("Validator missing from service provider.");

        await validator.ValidateAsync(this);
    }

    public async Task UseValidator<TValidator, TContext>(TContext context) where TValidator : IValidatorWithContext<TObject, TContext>
    {
        var validator =
            (TValidator?)serviceProvider.GetService(typeof(TValidator))
                ?? throw new InvalidOperationException("Validator missing from service provider.");

        await validator.ValidateAsync(this, context);
    }

    private static ResultMessage CreateMessage(string message, ResultMessageSeverity severity)
    {
        return severity switch
        {
            ResultMessageSeverity.Critical => ResultMessage.Critical(message),
            ResultMessageSeverity.Error => ResultMessage.Error(message),
            ResultMessageSeverity.NotFound => ResultMessage.NotFound(message),
            ResultMessageSeverity.Warning => ResultMessage.Warning(message),
            ResultMessageSeverity.Information => ResultMessage.Information(message),
            _ => throw new ArgumentOutOfRangeException(nameof(severity), "Invalid severity value.")
        };
    }

    private static ResultMessage CreateMessage<TPathValue>(Expression<Func<TObject, TPathValue>> path, string message, ResultMessageSeverity severity)
    {
        return severity switch
        {
            ResultMessageSeverity.Critical => ResultMessage.Critical(path, message),
            ResultMessageSeverity.Error => ResultMessage.Error(path, message),
            ResultMessageSeverity.NotFound => ResultMessage.NotFound(path),
            ResultMessageSeverity.Warning => ResultMessage.Warning(path, message),
            ResultMessageSeverity.Information => ResultMessage.Information(path, message),
            _ => throw new ArgumentOutOfRangeException(nameof(severity), "Invalid severity value.")
        };
    }
}
