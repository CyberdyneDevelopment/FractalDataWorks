using System;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Steps;

/// <summary>
/// A source of an OIDC authority's callback address (FDW-5730): it checks what the authority row states for it
/// when the row is read, and gives the callback address for the sign-in request in hand.
/// </summary>
/// <remarks>The same shape as the WebMcp self address sources (FDW-5721): one option per source, chosen by name on the row.</remarks>
public abstract class OidcRedirectAddressSourceBase : TypeOptionBase<OidcRedirectAddressSourceBase>
{
    /// <summary>Initializes the Empty/NotFound sentinel (source-generated).</summary>
    protected OidcRedirectAddressSourceBase()
        : base(string.Empty)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="OidcRedirectAddressSourceBase"/> class.</summary>
    /// <param name="name">The source's name, as an authority row's RedirectAddressSource states it.</param>
    /// <param name="displayName">The name shown to a person.</param>
    /// <param name="description">Where the callback address comes from.</param>
    protected OidcRedirectAddressSourceBase(string name, string displayName, string description)
        : base(name, $"OidcRedirectAddress:{name}", displayName, description, "OidcRedirectAddress")
    {
    }

    /// <summary>Gets whether this is the Empty/NotFound sentinel, returned for a name no option answers to.</summary>
    public bool IsEmpty => string.IsNullOrEmpty(Name);

    /// <summary>Checks what the authority row states beside this source, when the row is read.</summary>
    /// <param name="authority">The authority row's name, for the messages.</param>
    /// <param name="redirectUri">The row's RedirectUri, as stated.</param>
    /// <param name="allowedOrigins">The row's AllowedRedirectOrigins, as stated.</param>
    /// <param name="logger">The logger refusals are written to.</param>
    /// <returns>What the row states, checked; or the refusal naming what is wrong.</returns>
    public IGenericResult<OidcRedirectAddress> Check(string authority, string? redirectUri, string? allowedOrigins, ILogger logger)
        => CheckRow(authority, redirectUri, allowedOrigins, logger);

    /// <summary>Gives the callback address for the sign-in request in hand.</summary>
    /// <param name="configuration">The authority, as its row was read and checked.</param>
    /// <param name="context">The sign-in request, or <see langword="null"/> when none is in hand.</param>
    /// <param name="logger">The logger refusals are written to.</param>
    /// <returns>The absolute callback address, or the refusal naming why there is none.</returns>
    public IGenericResult<Uri> Resolve(IOidcAuthorityConfiguration configuration, HttpContext? context, ILogger logger)
        => ResolveAddress(configuration, context, logger);

    /// <summary>The source's own check of the row.</summary>
    /// <param name="authority">The authority row's name, for the messages.</param>
    /// <param name="redirectUri">The row's RedirectUri, as stated.</param>
    /// <param name="allowedOrigins">The row's AllowedRedirectOrigins, as stated.</param>
    /// <param name="logger">The logger refusals are written to.</param>
    /// <returns>What the row states, checked; or the refusal.</returns>
    /// <remarks>
    /// Virtual and refusing rather than abstract: the generated NotFound sentinel overrides abstract members as
    /// public, and this one is protected. Every source overrides it; the sentinel is refused as a source no option
    /// answers to.
    /// </remarks>
    protected virtual IGenericResult<OidcRedirectAddress> CheckRow(string authority, string? redirectUri, string? allowedOrigins, ILogger logger)
        => GenericResult<OidcRedirectAddress>.Failure(RedirectAddressLog.SourceUnknown(logger, authority, Name, OidcRedirectAddressSources.KnownNames()));

    /// <summary>The source's own step: gives the callback address for the request in hand.</summary>
    /// <param name="configuration">The authority, as its row was read and checked.</param>
    /// <param name="context">The sign-in request, or <see langword="null"/>.</param>
    /// <param name="logger">The logger refusals are written to.</param>
    /// <returns>The absolute callback address, or the refusal.</returns>
    /// <remarks>Virtual and refusing rather than abstract, for the same reason as <see cref="CheckRow"/>.</remarks>
    protected virtual IGenericResult<Uri> ResolveAddress(IOidcAuthorityConfiguration configuration, HttpContext? context, ILogger logger)
        => GenericResult<Uri>.Failure(RedirectAddressLog.SourceUnknown(logger, configuration.Name, Name, OidcRedirectAddressSources.KnownNames()));
}
