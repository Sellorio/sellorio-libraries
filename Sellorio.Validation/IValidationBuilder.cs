using Sellorio.Results.Messages;
using Sellorio.Validation.Validators;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

#pragma warning disable CS0618 // Type or member is obsolete (IValidator interfaces that shouldn't be used directly)

namespace Sellorio.Validation;

public interface IValidationBuilder<TObject>
{
    bool IsValid { get; }
    TObject Target { get; }

    IValidationBuilder<TObject> For<TNewObject>(
        Expression<Func<TObject, TNewObject>> path,
        Action<IValidationBuilder<TNewObject>> validate);

    IValidationBuilder<TObject> ForEach<TNewObject>(
        Expression<Func<TObject, IEnumerable<TNewObject>>> path,
        Action<IValidationBuilder<TNewObject>> validate);

    Task ForAsync<TNewObject>(
        Expression<Func<TObject, TNewObject>> path,
        Func<IValidationBuilder<TNewObject>, Task> validate);

    Task ForEachAsync<TNewObject>(
        Expression<Func<TObject, IEnumerable<TNewObject>>> path,
        Func<IValidationBuilder<TNewObject>, Task> validate);

    Task UseValidator<TValidator>()
        where TValidator : IValidatorWithoutContext<TObject>;

    Task UseValidator<TValidator, TContext>(TContext context)
        where TValidator : IValidatorWithContext<TObject, TContext>;

    IValidationBuilder<TObject> AddMessage(
        string message,
        ResultMessageSeverity severity = ResultMessageSeverity.Error);

    IValidationBuilder<TObject> AddMessage<TPathValue>(
        Expression<Func<TObject, TPathValue>> path,
        string message,
        ResultMessageSeverity severity = ResultMessageSeverity.Error);
}
