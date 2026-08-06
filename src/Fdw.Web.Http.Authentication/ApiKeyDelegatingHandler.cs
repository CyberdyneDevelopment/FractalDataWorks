namespace Fdw.Web.Http.Authentication;

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Web.Http.Authentication.Logging;
using Microsoft.Extensions.Logging;

/// <summary>
/// A delegating handler that attaches an API key header to outgoing HTTP requests.
/// Use this instead of <see cref="BearerTokenHandler"/> when the client is configured
/// with a static API key rather than JWT-based authentication.
/// </summary>
public sealed class ApiKeyDelegatingHandler : DelegatingHandler
{
    private readonly IApiKeyProvider _apiKeyProvider;
    private readonly ILogger<ApiKeyDelegatingHandler> _logger;

    /// <summary>
    /// The default header name for API key authentication.
    /// </summary>
    public const string DefaultHeaderName = "X-Api-Key";

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyDelegatingHandler"/> class.
    /// </summary>
    /// <param name="apiKeyProvider">The API key provider.</param>
    /// <param name="logger">The logger.</param>
    public ApiKeyDelegatingHandler(
        IApiKeyProvider apiKeyProvider,
        ILogger<ApiKeyDelegatingHandler> logger)
    {
        _apiKeyProvider = apiKeyProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? string.Empty;

        var apiKey = await _apiKeyProvider.GetApiKey(cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrEmpty(apiKey))
        {
            request.Headers.TryAddWithoutValidation(DefaultHeaderName, apiKey);
            ApiKeyLog.ApiKeyAttached(_logger, path);
        }
        else
        {
            ApiKeyLog.NoApiKeyAvailable(_logger, path);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
