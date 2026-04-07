using System;
using System.Threading.Tasks;
using Sellorio.Results;

namespace Sellorio.Validation;

public interface IValidationService
{
    Result<TObject> Validate<TObject>(TObject obj, Action<IValidationBuilder<TObject>> validate);
    Task<Result<TObject>> ValidateAsync<TObject>(TObject obj, Func<IValidationBuilder<TObject>, Task> validate);
}
