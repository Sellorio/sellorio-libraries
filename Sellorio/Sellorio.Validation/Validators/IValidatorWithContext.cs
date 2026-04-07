using System;
using System.Threading.Tasks;

namespace Sellorio.Validation.Validators;

[Obsolete("Do not inherit directly from this type. Use IValidator instead.")]
public interface IValidatorWithContext<TObject, TContext>
{
    Task ValidateAsync(IValidationBuilder<TObject> validate, TContext context);
}
