using System.Threading;
using System.Threading.Tasks;

namespace Sellorio.Clients.Rest.Authentication;

public interface IAuthenticationTokenSource
{
    Task<string?> GetTokenAsync(CancellationToken cancellationToken);
}
