using System.Net.Http;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.Clients.Abstractions.Tests;

internal sealed class TestApiClient : ApiClientBase
{
    public TestApiClient(HttpClient httpClient, ILogger<ApiClientBase> logger)
        : base(httpClient, logger) { }

    public new Task<IGenericResult<T>> Get<T>(string path, CancellationToken ct = default)
        => base.Get<T>(path, ct);

    public new Task<IGenericResult<TResponse>> Post<TRequest, TResponse>(string path, TRequest request, CancellationToken ct = default)
        => base.Post<TRequest, TResponse>(path, request, ct);

    public new Task<IGenericResult> Post<TRequest>(string path, TRequest request, CancellationToken ct = default)
        => base.Post<TRequest>(path, request, ct);

    public new Task<IGenericResult> Post(string path, CancellationToken ct = default)
        => base.Post(path, ct);

    public new Task<IGenericResult<TResponse>> PostWithResponse<TResponse>(string path, CancellationToken ct = default)
        => base.PostWithResponse<TResponse>(path, ct);

    public new Task<IGenericResult<TResponse>> Put<TRequest, TResponse>(string path, TRequest request, CancellationToken ct = default)
        => base.Put<TRequest, TResponse>(path, request, ct);

    public new Task<IGenericResult> Put<TRequest>(string path, TRequest request, CancellationToken ct = default)
        => base.Put<TRequest>(path, request, ct);

    public new Task<IGenericResult<TResponse>> Patch<TRequest, TResponse>(string path, TRequest request, CancellationToken ct = default)
        => base.Patch<TRequest, TResponse>(path, request, ct);

    public new Task<IGenericResult> Delete(string path, CancellationToken ct = default)
        => base.Delete(path, ct);
}
