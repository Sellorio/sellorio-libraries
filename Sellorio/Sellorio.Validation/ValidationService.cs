using Sellorio.Results;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Sellorio.Validation;

internal class ValidationService(IServiceProvider serviceProvider) : IValidationService
{
    public Result<TObject> Validate<TObject>(TObject obj, Action<IValidationBuilder<TObject>> validate)
    {
        var validationBuilder = new ValidationBuilder<TObject>(obj, serviceProvider);

        try
        {
            validate.Invoke(validationBuilder);
        }
        catch (FastFailException)
        {
        }

        return new Result<TObject>(validationBuilder.Messages.ToImmutableArray());
    }

    public async Task<Result<TObject>> ValidateAsync<TObject>(TObject obj, Func<IValidationBuilder<TObject>, Task> validate)
    {
        var validationBuilder = new ValidationBuilder<TObject>(obj, serviceProvider);

        try
        {
            await validate.Invoke(validationBuilder);
        }
        catch (FastFailException)
        {
        }

        return new Result<TObject>(validationBuilder.Messages.ToImmutableArray());
    }
}
