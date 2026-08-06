namespace Fdw.Web.Http.Authentication;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Web.Http.Authentication.Logging;
using Microsoft.Extensions.Logging;

/// <summary>
/// A delegating handler that attaches a bearer token to outgoing HTTP requests
/// and retries once with a refreshed token on 401 Unauthorized responses.
/// </summary>
public sealed class RetryingBearerTokenHandler : DelegatingHandler
{
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly ITokenRefreshHandler _refreshHandler;
    private readonly IAuthExpirationNotifier? _expirationNotifier;
    private readonly ILogger<RetryingBearerTokenHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryingBearerTokenHandler"/> class.
    /// </summary>
    /// <param name="tokenProvider">The access token provider.</param>
    /// <param name="refreshHandler">The token refresh handler.</param>
    /// <param name="expirationNotifier">The optional session expiration notifier.</param>
    /// <param name="logger">The logger.</param>
    public RetryingBearerTokenHandler(
        IAccessTokenProvider tokenProvider,
        ITokenRefreshHandler refreshHandler,
        IAuthExpirationNotifier? expirationNotifier,
        ILogger<RetryingBearerTokenHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(tokenProvider);
        ArgumentNullException.ThrowIfNull(refreshHandler);
        ArgumentNullException.ThrowIfNull(logger);

        _tokenProvider = tokenProvider;
        _refreshHandler = refreshHandler;
        _expirationNotifier = expirationNotifier;
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
        }

        var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.Unauthorized && _refreshHandler.CanRefresh)
        {
            BearerTokenLog.UnauthorizedRefreshAttempt(_logger, path);

            var refreshed = await _refreshHandler.TryRefresh(cancellationToken).ConfigureAwait(false);

            if (refreshed)
            {
                BearerTokenLog.TokenRefreshedRetrying(_logger, path);

                var newToken = await _tokenProvider.GetAccessToken(cancellationToken).ConfigureAwait(false);

                using var retryRequest = CloneHttpRequestMessage(request);

                if (!string.IsNullOrEmpty(newToken))
                {
                    retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                }

                response.Dispose();
                response = await base.SendAsync(retryRequest, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                BearerTokenLog.TokenRefreshFailed(_logger, path);

                if (_expirationNotifier is not null)
                {
                    await _expirationNotifier.NotifySessionExpired().ConfigureAwait(false);
                    BearerTokenLog.SessionExpiredNotificationSent(_logger, path);
                }
            }
        }

        return response;
    }

    private static HttpRequestMessage CloneHttpRequestMessage(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
        };

        if (request.Content is not null)
        {
            clone.Content = request.Content;
        }

        foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

#if NET5_0_OR_GREATER
        foreach (KeyValuePair<string, object?> option in request.Options)
        {
            clone.Options.TryAdd(option.Key, option.Value);
        }
#endif

        return clone;
    }
}
