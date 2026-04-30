using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sellorio.Clients.Rest.Authentication;

public interface IAuthenticationTokenHandler
{
    Task ConfigureTokenAsync(HttpRequestMessage request, string token, CancellationToken cancellationToken);
}
