using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Authentication.Logging;

/// <summary>
/// MessageLogging for where an OIDC authority's callback address comes from (FDW-5730).
/// </summary>
/// <remarks>
/// EventIds are categorized numbers (<c>Category = Id / 10000</c>), so each one also chooses the answer's status:
/// 61012-61020 the authority row (Configuration, 500: the host's settings are wrong, not the caller), 21290-21291 a
/// sign-in request with no usable host or scheme (Validation, 400), 91290 a sign-in with no request in hand
/// (Internal, 500), 101290 a host not on the row's list (Forbidden, 403), and 11012 the trace of a resolved address.
/// Each reason has its own number, so a refusal names why without reading the text.
/// </remarks>
[ExcludeFromCodeCoverage(Justification = "MessageLogging partial class - implementation is source-generated")]
[MessageLoggingTypeCode("AUTHENTICATION")]
internal static partial class RedirectAddressLog
{
    /// <summary>The row names no source for its callback address.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="authority">The authority row's name.</param>
    /// <param name="known">The sources there are.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 61012, Level = LogLevel.Error,
        Message = "OIDC authority '{authority}' names no RedirectAddressSource, so it does not say where its callback address comes from. Name one of: {known}. Nothing is assumed")]
    internal static partial IGenericMessage SourceNotStated(ILogger logger, string authority, string known);

    /// <summary>The row names a source no option answers to.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="authority">The authority row's name.</param>
    /// <param name="source">The name the row states.</param>
    /// <param name="known">The sources there are.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 61013, Level = LogLevel.Error,
        Message = "OIDC authority '{authority}' names RedirectAddressSource '{source}', and no source answers to it. The sources are: {known}")]
    internal static partial IGenericMessage SourceUnknown(ILogger logger, string authority, string source, string known);

    /// <summary>A RequestHost row lists no origins.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="authority">The authority row's name.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 61014, Level = LogLevel.Error,
        Message = "OIDC authority '{authority}' takes its callback from the request's host (RequestHost) and lists no AllowedRedirectOrigins. A host taken from a request is trusted only when it is listed, so with no list every sign-in would be refused; there is no wildcard and no default list")]
    internal static partial IGenericMessage OriginsMissing(ILogger logger, string authority);

    /// <summary>An entry of the row's origin list is not a plain origin.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="authority">The authority row's name.</param>
    /// <param name="entry">The entry as stated.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 61015, Level = LogLevel.Error,
        Message = "OIDC authority '{authority}' lists AllowedRedirectOrigins entry '{entry}', which is not an origin. Each entry is an absolute http or https scheme://host[:port] with no path, query, fragment or user info, and no wildcard")]
    internal static partial IGenericMessage OriginInvalid(ILogger logger, string authority, string entry);

    /// <summary>A RequestHost row's RedirectUri is not a root-relative callback path.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="authority">The authority row's name.</param>
    /// <param name="value">The RedirectUri as stated.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 61016, Level = LogLevel.Error,
        Message = "OIDC authority '{authority}' takes its callback from the request's host (RequestHost), and the callback path its RedirectUri '{value}' gives is not usable. The path, stated alone or as the path of an absolute address, starts with a single '/' and has no query or fragment: a path starting '//' would be read as another host")]
    internal static partial IGenericMessage CallbackPathInvalid(ILogger logger, string authority, string value);

    /// <summary>A RequestHost row's RedirectUri is neither a callback path nor an absolute http or https address.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="authority">The authority row's name.</param>
    /// <param name="value">The RedirectUri as stated.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 61019, Level = LogLevel.Error,
        Message = "OIDC authority '{authority}' takes its callback from the request's host (RequestHost), and its RedirectUri '{value}' is neither a callback path (starting with '/') nor an absolute http or https address, so it gives no callback path")]
    internal static partial IGenericMessage RedirectUriUnreadable(ILogger logger, string authority, string value);

    /// <summary>A RequestHost row's absolute RedirectUri names an origin its list does not.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="authority">The authority row's name.</param>
    /// <param name="origin">The origin of the stored RedirectUri.</param>
    /// <param name="allowed">The origins the row lists.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 61020, Level = LogLevel.Error,
        Message = "OIDC authority '{authority}' takes its callback from the request's host (RequestHost), and its absolute RedirectUri names origin '{origin}', which is not one of its AllowedRedirectOrigins ({allowed}). Only the path of that address is used, but the address is where an older build sends people, so it must be a callback the row trusts: list its origin")]
    internal static partial IGenericMessage StoredOriginNotListed(ILogger logger, string authority, string origin, string allowed);

    /// <summary>A FixedAddress row's RedirectUri is not an absolute http or https address.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="authority">The authority row's name.</param>
    /// <param name="value">The RedirectUri as stated.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 61017, Level = LogLevel.Error,
        Message = "OIDC authority '{authority}' uses a fixed callback address (FixedAddress), and its RedirectUri '{value}' is not an absolute http or https address. RFC 9700 requires an exact registered match, so it cannot be inferred")]
    internal static partial IGenericMessage FixedAddressInvalid(ILogger logger, string authority, string value);

    /// <summary>A FixedAddress row also lists origins, which it does not read.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="authority">The authority row's name.</param>
    /// <param name="origins">The list as stated.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 61018, Level = LogLevel.Error,
        Message = "OIDC authority '{authority}' uses a fixed callback address (FixedAddress) and also lists AllowedRedirectOrigins '{origins}', which that source does not read. Leave the list empty, or name RequestHost to use it")]
    internal static partial IGenericMessage OriginsNotRead(ILogger logger, string authority, string origins);

    /// <summary>A sign-in asked for a request-host callback with no request in hand.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="authority">The authority row's name.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 91290, Level = LogLevel.Error,
        Message = "OIDC authority '{authority}' takes its callback from the request's host (RequestHost), and the sign-in ran with no HTTP request in hand, so there is no host to take")]
    internal static partial IGenericMessage RequestUnavailable(ILogger logger, string authority);

    /// <summary>The sign-in request carries no usable host.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="authority">The authority row's name.</param>
    /// <param name="host">The host as the request carries it.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 21290, Level = LogLevel.Warning,
        Message = "OIDC authority '{authority}' takes its callback from the request's host (RequestHost), and the sign-in request carries no usable host ('{host}')")]
    internal static partial IGenericMessage HostUnusable(ILogger logger, string authority, string host);

    /// <summary>The sign-in request's scheme is not http or https.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="authority">The authority row's name.</param>
    /// <param name="scheme">The scheme as the forwarded-headers middleware left it.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 21291, Level = LogLevel.Warning,
        Message = "OIDC authority '{authority}' takes its callback from the request's host (RequestHost), and the sign-in request's scheme '{scheme}' is not http or https")]
    internal static partial IGenericMessage SchemeUnusable(ILogger logger, string authority, string scheme);

    /// <summary>The sign-in request's origin is not on the row's list.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="authority">The authority row's name.</param>
    /// <param name="origin">The request's origin.</param>
    /// <param name="allowed">The origins the row lists.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 101290, Level = LogLevel.Warning,
        Message = "OIDC authority '{authority}' refused a sign-in from origin '{origin}': it is not one of the AllowedRedirectOrigins ({allowed}), so no callback is built on it. A host taken from a request is trusted only when it is listed")]
    internal static partial IGenericMessage OriginNotAllowed(ILogger logger, string authority, string origin, string allowed);

    /// <summary>A callback address was resolved.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="authority">The authority row's name.</param>
    /// <param name="source">The source the row names.</param>
    /// <param name="address">The callback address.</param>
    /// <returns>The structured message.</returns>
    [MessageLogging(EventId = 11012, Level = LogLevel.Trace,
        Message = "OIDC authority '{authority}' callback resolved from source '{source}': {address}")]
    internal static partial IGenericMessage Resolved(ILogger logger, string authority, string source, string address);
}
