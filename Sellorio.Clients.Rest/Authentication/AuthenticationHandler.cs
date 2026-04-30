using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sellorio.Clients.Rest.Authentication;

internal class AuthenticationHandler
{
    private readonly IAuthenticationTokenSource? _authenticationTokenSource;
    private readonly IAuthenticationTokenHandler? _authenticationTokenHandler;
    private readonly bool _isEnabled;

    public AuthenticationHandler(
        IAuthenticationTokenSource? authenticationTokenSource = null,
        IAuthenticationTokenHandler? authenticationTokenHandler = null)
    {
        _authenticationTokenSource = authenticationTokenSource;
        _authenticationTokenHandler = authenticationTokenHandler;

        if (_authenticationTokenHandler != null && _authenticationTokenSource == null)
        {
            throw new InvalidOperationException("Missing authentication token source.");
        }

        _isEnabled = _authenticationTokenHandler != null;
    }

    public async Task ConfigureRequestMessageAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_isEnabled)
        {
            var token = await _authenticationTokenSource!.GetTokenAsync(cancellationToken);

            if (token != null)
            {
                await _authenticationTokenHandler!.ConfigureTokenAsync(request, token, cancellationToken);
            }
        }
    }
}
