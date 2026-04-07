namespace Sellorio.Validation.Validators;

#pragma warning disable CS0618 // Type or member is obsolete (IValidator interfaces that shouldn't be used directly)

public interface IValidator<TObject> : IValidatorWithoutContext<TObject>
{
}

public interface IValidator<TObject, TContext> : IValidatorWithContext<TObject, TContext>
{
}
