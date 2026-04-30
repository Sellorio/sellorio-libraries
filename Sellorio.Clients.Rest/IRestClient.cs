using System;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sellorio.Clients.Rest;

public interface IRestClient
{
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Delete(FormattableString url, CancellationToken cancellationToken = default);
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Get(FormattableString url, CancellationToken cancellationToken = default);
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Patch(FormattableString url, CancellationToken cancellationToken = default);
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Patch(FormattableString url, object body, CancellationToken cancellationToken = default);
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Patch(FormattableString url, Stream file, CancellationToken cancellationToken = default);
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Post(FormattableString url, CancellationToken cancellationToken = default);
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Post(FormattableString url, object body, CancellationToken cancellationToken = default);
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Post(FormattableString url, Stream file, CancellationToken cancellationToken = default);
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Put(FormattableString url, CancellationToken cancellationToken = default);
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Put(FormattableString url, object body, CancellationToken cancellationToken = default);
    [OverloadResolutionPriority(1)]
    Task<HttpResponseMessage> Put(FormattableString url, Stream file, CancellationToken cancellationToken = default);

    Task<HttpResponseMessage> Delete(string url, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> Get(string url, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> Patch(string url, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> Patch(string url, object body, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> Patch(string url, Stream file, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> Post(string url, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> Post(string url, object body, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> Post(string url, Stream file, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> Put(string url, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> Put(string url, object body, CancellationToken cancellationToken = default);
    Task<HttpResponseMessage> Put(string url, Stream file, CancellationToken cancellationToken = default);
}
