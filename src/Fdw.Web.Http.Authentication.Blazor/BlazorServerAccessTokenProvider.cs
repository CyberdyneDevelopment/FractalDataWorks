namespace Fdw.Web.Http.Authentication.Blazor;

using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Web.Http.Authentication.Blazor.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Provides access tokens for Blazor Server applications by reading from the
/// <see cref="CircuitTokenAccessor"/> first, falling back to <see cref="IHttpContextAccessor"/>
/// for SSR prerender requests where HttpContext is still valid.
/// </summary>
public sealed class BlazorServerAccessTokenProvider : IAccessTokenProvider
{
    private readonly CircuitTokenAccessor _circuitTokenAccessor;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<BlazorServerAccessTokenProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorServerAccessTokenProvider"/> class.
    /// </summary>
    /// <param name="circuitTokenAccessor">The circuit token accessor.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor.</param>
    /// <param name="logger">The logger.</param>
    public BlazorServerAccessTokenProvider(
        CircuitTokenAccessor circuitTokenAccessor,
        IHttpContextAccessor httpContextAccessor,
        ILogger<BlazorServerAccessTokenProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(circuitTokenAccessor);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(logger);

        _circuitTokenAccessor = circuitTokenAccessor;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessToken(CancellationToken cancellationToken = default)
    {
        var token = _circuitTokenAccessor.CurrentToken;

        if (!string.IsNullOrEmpty(token))
        {
            BlazorAuthLog.TokenFromCircuit(_logger);
            return token;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            try
            {
                token = await httpContext.GetTokenAsync("access_token").ConfigureAwait(false);
                if (!string.IsNullOrEmpty(token))
                {
                    BlazorAuthLog.TokenFromHttpContext(_logger);
                    return token;
                }
            }
            catch (Exception ex)
            {
                BlazorAuthLog.TokenRetrievalError(_logger, ex);
            }
        }

        BlazorAuthLog.NoTokenAvailable(_logger);
        return null;
    }
}
