using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Sellorio.Clients.Rest;

internal interface IRestClient
{
    Task<HttpResponseMessage> Delete(FormattableString url);
    Task<HttpResponseMessage> Get(FormattableString url);
    Task<HttpResponseMessage> Patch(FormattableString url);
    Task<HttpResponseMessage> Patch(FormattableString url, object body);
    Task<HttpResponseMessage> Patch(FormattableString url, Stream file);
    Task<HttpResponseMessage> Post(FormattableString url);
    Task<HttpResponseMessage> Post(FormattableString url, object body);
    Task<HttpResponseMessage> Post(FormattableString url, Stream file);
    Task<HttpResponseMessage> Put(FormattableString url);
    Task<HttpResponseMessage> Put(FormattableString url, object body);
    Task<HttpResponseMessage> Put(FormattableString url, Stream file);
}
