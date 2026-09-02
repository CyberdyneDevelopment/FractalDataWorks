using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Fdw.Services.Authentication.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Validation;

/// <summary>
/// Picks the authentication scheme for a request from the issuer of the bearer token it carries.
/// </summary>
/// <remarks>
/// <para>
/// A host that trusts more than one issuer has more than one validation scheme, and only one of them
/// can validate any given token: a scheme configured for issuer A rejects a token from issuer B before
/// it looks at the signature. ASP.NET picks ONE default scheme per request, so something has to decide
/// which — and the only thing in the request that says is the token's own <c>iss</c>.
/// </para>
/// <para>
/// The claim is read WITHOUT validating anything, which is safe because nothing is trusted on the
/// strength of it: it selects which scheme gets to validate, and that scheme then checks issuer,
/// signature, audience and lifetime itself. A forged <c>iss</c> routes a token to a scheme that
/// rejects it.
/// </para>
/// </remarks>
public static class IssuerSchemeSelector
{
    /// <summary>The scheme name of the policy scheme that does the selecting.</summary>
    public const string SchemeName = "Fdw.IssuerSelector";

    private const string BearerPrefix = "Bearer ";

    /// <summary>
    /// Returns the scheme that should authenticate <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The request.</param>
    /// <returns>
    /// The scheme declared for the token's issuer, or <see cref="UnmatchedIssuerHandler.SchemeName"/>
    /// when the request carries no readable bearer token or names an issuer no scheme accepts. That
    /// scheme fails the request rather than passing it to some other scheme's validator: routing an
    /// unrecognised issuer to a validator that was not declared for it is the guess this design exists
    /// to remove.
    /// </returns>
    public static string Select(HttpContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));

        var logger = context.RequestServices.GetService<ILoggerFactory>()?.CreateLogger(typeof(IssuerSchemeSelector))
            ?? (ILogger)NullLogger.Instance;

        var authorization = context.Request.Headers.Authorization.ToString();

        // An opaque credential is recognised by its prefix, because there is nothing in it to read.
        // It routes under a well-known issuer through the SAME binding lookup as every other scheme,
        // rather than short-circuiting to a scheme name: a second routing path would have to be kept
        // in step with this one, and a host that has not declared the ApiKey entry should fall
        // through to the unmatched handler exactly as it would for an undeclared issuer.
        var issuer = authorization.StartsWith(
            ApiKeyAuthenticationType.CredentialPrefix, StringComparison.OrdinalIgnoreCase)
                ? OpaqueCredentialIssuerFor(context, logger)
                : ReadIssuer(authorization, context.Request.Path.ToString(), logger);

        if (issuer is null)
        {
            AuthenticationValidationLog.NoReadableBearerToken(logger, context.Request.Path.ToString());
            return UnmatchedIssuerHandler.SchemeName;
        }

        // From the registry rather than from DI: the entries that decide these bindings are read
        // through a gateway during Initialize, which is after the container is built, so they cannot
        // be service registrations made while it was still being described.
        var bindings = context.RequestServices.GetRequiredService<AuthenticationSchemeBindings>().All;

        var match = bindings.FirstOrDefault(
            b => string.Equals(b.Issuer, issuer, StringComparison.Ordinal));

        if (match is null)
        {
            AuthenticationValidationLog.IssuerNotDeclared(
                logger, issuer, string.Join(", ", bindings.Select(b => b.Issuer)), context.Request.Path.ToString());
            return UnmatchedIssuerHandler.SchemeName;
        }

        AuthenticationValidationLog.IssuerRouted(logger, issuer, match.SchemeName, match.ServiceName);
        return match.SchemeName;
    }

    private static string OpaqueCredentialIssuerFor(HttpContext context, ILogger logger)
    {
        AuthenticationValidationLog.OpaqueCredentialRouted(logger, context.Request.Path.ToString());
        return ApiKeyAuthenticationType.OpaqueCredentialIssuer;
    }

    private static string? ReadIssuer(string authorizationHeader, string path, ILogger logger)
    {
        if (string.IsNullOrEmpty(authorizationHeader)
            || !authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var parts = authorizationHeader[BearerPrefix.Length..].Trim().Split('.');
        if (parts.Length != 3)
            return null;

        try
        {
            using var payload = JsonDocument.Parse(DecodeSegment(parts[1]));
            return payload.RootElement.TryGetProperty("iss", out var iss) && iss.ValueKind == JsonValueKind.String
                ? iss.GetString()
                : null;
        }
        catch (FormatException ex)
        {
            AuthenticationValidationLog.BearerTokenPayloadUnreadable(logger, ex, path);
            return null;
        }
        catch (JsonException ex)
        {
            AuthenticationValidationLog.BearerTokenPayloadUnreadable(logger, ex, path);
            return null;
        }
    }

    private static byte[] DecodeSegment(string segment)
    {
        var padded = segment.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(padded.PadRight(padded.Length + ((4 - (padded.Length % 4)) % 4), '='));
    }
}
