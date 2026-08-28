namespace Fdw.Web.Http.Authentication;

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Web.Http.Authentication.Logging;
using Microsoft.Extensions.Logging;

/// <summary>
/// A delegating handler that attaches a bearer token to outgoing HTTP requests.
/// </summary>
public sealed class BearerTokenHandler : DelegatingHandler
{
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly ILogger<BearerTokenHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BearerTokenHandler"/> class.
    /// </summary>
    /// <param name="tokenProvider">The access token provider.</param>
    /// <param name="logger">The logger.</param>
    public BearerTokenHandler(
        IAccessTokenProvider tokenProvider,
        ILogger<BearerTokenHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(tokenProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _tokenProvider = tokenProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? string.Empty;

        try
        {
            var token = await _tokenProvider.GetAccessToken(cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                BearerTokenLog.TokenAttached(_logger, path);
            }
            else
            {
                BearerTokenLog.NoTokenAvailable(_logger, path);
            }
        }
        catch (Exception ex)
        {
            BearerTokenLog.TokenAttachmentError(_logger, ex, path);
            throw;
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
