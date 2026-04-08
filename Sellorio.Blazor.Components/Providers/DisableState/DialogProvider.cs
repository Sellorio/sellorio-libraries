using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MudBlazor;
using Sellorio.Results;
using Sellorio.Results.Messages;

namespace Sellorio.Blazor.Components.Providers.DisableState;

internal class DialogProvider(DisableStateScope disableStateScope, IDialogService dialogService) : IDialogProvider
{
    public async Task<Result> ShowDialogAsync(Expression<Func<AoDialogBase>> expression)
    {
        var result = await ShowDialogInternalAsync(expression);
        return result.AsResult();
    }

    public async Task<ValueResult<TResult>> ShowDialogAsync<TResult>(Expression<Func<AoDialogBase<TResult>>> expression)
    {
        var result = await ShowDialogInternalAsync(expression);

        if (!result.WasSuccess)
        {
            return ValueResult<TResult>.Failure(result.Messages);
        }

        return (TResult)result.Value.Data!;
    }

    private async Task<ValueResult<DialogResult>> ShowDialogInternalAsync(LambdaExpression expression)
    {
        if (disableStateScope.IsDisabled)
        {
            return ResultMessage.Error("Cannot create a dialog since the current disable scope is disabled.");
        }

        var memberInitExpression = (MemberInitExpression)expression.Body;
        var bindings = memberInitExpression.Bindings;
        var dialogParameters = new DialogParameters();

        foreach (var binding in bindings.Cast<MemberAssignment>())
        {
            var value = Expression.Lambda(binding.Expression).Compile().DynamicInvoke();
            dialogParameters.Add(binding.Member.Name, value);
        }

        DialogResult result;

        try
        {
            disableStateScope.UpdateState(true);
            var dialog = await dialogService.ShowAsync(memberInitExpression.NewExpression.Constructor!.DeclaringType!, null, dialogParameters);
            result = (await dialog.Result)!;
        }
        finally
        {
            disableStateScope.UpdateState(false);
        }

        if (result.Canceled)
        {
            return ResultMessage.Error("Action was cancelled.");
        }

        return result;
    }
}
