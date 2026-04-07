using System.Threading.Tasks;
using Sellorio.Validation;

namespace Sellorio.Validation.Validators;

public abstract class AttributeValidator<TObject> : IValidator<TObject>
{
    public Task ValidateAsync(IValidationBuilder<TObject> validate)
    {
        validate.Attributes();
        return Task.CompletedTask;
    }
}
