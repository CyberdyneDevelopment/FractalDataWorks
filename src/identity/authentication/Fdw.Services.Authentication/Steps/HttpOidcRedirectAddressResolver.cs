using System;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Steps;

/// <summary>
/// Gives an OIDC sign-in's callback address through the source the authority row names (FDW-5730), with the
/// request in hand read through the HTTP context accessor at the moment it is asked.
/// </summary>
/// <remarks>
/// A singleton like <see cref="HttpOidcCallbackAccessor"/>: it holds no request, so one instance follows every
/// request (FDW-4640).
/// </remarks>
public sealed class HttpOidcRedirectAddressResolver : IOidcRedirectAddressResolver
{
    private readonly IHttpContextAccessor _context;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="HttpOidcRedirectAddressResolver"/> class.</summary>
    /// <param name="context">Supplies the current request.</param>
    /// <param name="logger">The logger.</param>
    public HttpOidcRedirectAddressResolver(IHttpContextAccessor context, ILogger<HttpOidcRedirectAddressResolver>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<HttpOidcRedirectAddressResolver>.Instance;
    }

    /// <inheritdoc />
    public IGenericResult<Uri> Resolve(IOidcAuthorityConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.RedirectAddressSource))
        {
            return GenericResult<Uri>.Failure(RedirectAddressLog.SourceNotStated(_logger, configuration.Name, OidcRedirectAddressSources.KnownNames()));
        }

        var source = OidcRedirectAddressSources.ByName(configuration.RedirectAddressSource);
        if (source.IsEmpty)
        {
            return GenericResult<Uri>.Failure(RedirectAddressLog.SourceUnknown(
                _logger, configuration.Name, configuration.RedirectAddressSource, OidcRedirectAddressSources.KnownNames()));
        }

        var address = source.Resolve(configuration, _context.HttpContext, _logger);
        if (address.IsSuccess)
        {
            RedirectAddressLog.Resolved(_logger, configuration.Name, source.Name, address.Value!.AbsoluteUri);
        }

        return address;
    }
}
