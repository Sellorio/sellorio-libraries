using System;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Sellorio.Clients.Rest;

public interface IRestClient
{
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Delete(FormattableString url);
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Get(FormattableString url);
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Patch(FormattableString url);
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Patch(FormattableString url, object body);
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Patch(FormattableString url, Stream file);
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Post(FormattableString url);
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Post(FormattableString url, object body);
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Post(FormattableString url, Stream file);
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Put(FormattableString url);
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Put(FormattableString url, object body);
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Put(FormattableString url, Stream file);

    Task<HttpResponseMessage> Delete(string url);
    Task<HttpResponseMessage> Get(string url);
    Task<HttpResponseMessage> Patch(string url);
    Task<HttpResponseMessage> Patch(string url, object body);
    Task<HttpResponseMessage> Patch(string url, Stream file);
    Task<HttpResponseMessage> Post(string url);
    Task<HttpResponseMessage> Post(string url, object body);
    Task<HttpResponseMessage> Post(string url, Stream file);
    Task<HttpResponseMessage> Put(string url);
    Task<HttpResponseMessage> Put(string url, object body);
    Task<HttpResponseMessage> Put(string url, Stream file);
}
