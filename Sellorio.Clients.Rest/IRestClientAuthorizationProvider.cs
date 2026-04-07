using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Sellorio.Clients.Rest;

public interface IRestClientAuthorizationProvider
{
    Task<AuthenticationHeaderValue?> GetAuthorizationHeaderAsync();
}
