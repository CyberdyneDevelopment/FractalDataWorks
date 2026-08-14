using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.ServiceTypes;
using Fdw.Services.Identity.Abstractions;
using Fdw.Services.Identity.Endpoints.Logging;
using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Identity.Endpoints;

/// <summary>
/// Asks a configured identity to prove itself against the real identity provider, and reports what
/// happened without returning the token.
/// </summary>
/// <remarks>
/// <para>
/// This is the endpoint an operator actually needs. A managed identity fails in ways that are
/// invisible until the call it was meant to authorize fails somewhere else — a rotated client secret,
/// a revoked service account, an assertion the provider stopped trusting, a scope quietly narrowed.
/// This turns all of that into one answer at the moment of asking.
/// </para>
/// <para>
/// <b>It never returns the token.</b> Issuer, audience, granted scopes and expiry are what diagnose a
/// failure; the token itself would let anyone who can call this endpoint impersonate the service, and
/// a diagnostic that hands out credentials is a worse problem than the one it diagnoses. The failure
/// reasons stay distinct for the same reason — "no such configuration", "provider rejected our
/// credential" and "provider unreachable" need different fixes.
/// </para>
/// <para>
/// It deliberately goes through the acquisition path rather than a bespoke probe, so what it verifies
/// is the same code an outbound call uses. A probe that tested something adjacent would pass while
/// the real path failed.
/// </para>
/// </remarks>
public abstract class VerifyIdentityEndpointBase : Endpoint<VerifyIdentityRequest, VerifyIdentityResponse>
{
    /// <summary>Gets the provider identities are resolved through.</summary>
    protected abstract IFdwServiceProvider<IIdentityService, IdentityServiceConfiguration> Identities { get; }

    /// <summary>Gets the route this endpoint is served at.</summary>
    protected virtual string Route => "/identities/{Name}/verify";

    /// <summary>Gets the authorization policy required to verify an identity.</summary>
    /// <remarks>
    /// Why the write policy and not the read one: verifying causes a real token to be minted at the
    /// provider, which is an action with a rate limit, an audit trail and a cost — not a read.
    /// </remarks>
    protected virtual string VerifyPolicy => "identities:write";

    /// <inheritdoc/>
    public override void Configure()
    {
        Post(Route);
        Policies(VerifyPolicy);
        Summary(s =>
        {
            s.Summary = "Verify a managed identity can obtain a token";
            s.Description =
                "Asks the identity to authenticate against its configured provider and reports the "
                + "issuer, audience, granted scopes and expiry. Never returns the token itself.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(VerifyIdentityRequest req, CancellationToken ct)
    {
        if (req.Name is not { Length: > 0 })
        {
            IdentityEndpointLog.VerifyRequestIncomplete(Logger, nameof(req.Name));
            await Send.ResponseAsync(Failed(req, nameof(req.Name)), 400, ct).ConfigureAwait(false);
            return;
        }

        if (req.Audience is not { Length: > 0 })
        {
            // NO FALLBACKS: an audience is what bounds the token, so there is nothing sensible to
            // assume when one is not supplied.
            IdentityEndpointLog.VerifyRequestIncomplete(Logger, nameof(req.Audience));
            await Send.ResponseAsync(Failed(req, nameof(req.Audience)), 400, ct).ConfigureAwait(false);
            return;
        }

        IdentityEndpointLog.VerifyingIdentity(Logger, req.Name, req.Audience);

        var identity = await Identities.Get(req.Name, ct).ConfigureAwait(false);
        if (!identity.IsSuccess || identity.Value is not { } service)
        {
            IdentityEndpointLog.IdentityVerificationFailed(Logger, req.Name, req.Audience, identity.CurrentMessage ?? "not found");
            await Send.ResponseAsync(
                new VerifyIdentityResponse
                {
                    Succeeded = false,
                    Name = req.Name,
                    Audience = req.Audience,
                    Failure = identity.CurrentMessage,
                },
                404, ct).ConfigureAwait(false);
            return;
        }

        var acquired = await service
            .Acquire(new IdentityTokenRequest(req.Audience, SplitScopes(req.Scopes)), ct)
            .ConfigureAwait(false);

        if (!acquired.IsSuccess || acquired.Value is not { } token)
        {
            IdentityEndpointLog.IdentityVerificationFailed(Logger, req.Name, req.Audience, acquired.CurrentMessage ?? "no token");
            await Send.ResponseAsync(
                new VerifyIdentityResponse
                {
                    Succeeded = false,
                    Name = req.Name,
                    Mechanism = service.ServiceType,
                    Audience = req.Audience,
                    Failure = acquired.CurrentMessage,
                },
                // Why 502 and not 500: the failure is the upstream provider's answer, not this
                // service malfunctioning, and an operator reading a dashboard needs that distinction.
                502, ct).ConfigureAwait(false);
            return;
        }

        IdentityEndpointLog.IdentityVerified(Logger, req.Name, token.Issuer, token.Audience);

        // token.Value is deliberately not read here.
        await Send.OkAsync(
            new VerifyIdentityResponse
            {
                Succeeded = true,
                Name = req.Name,
                Mechanism = service.ServiceType,
                Issuer = token.Issuer,
                Audience = token.Audience,
                GrantedScopes = token.Scopes,
                ExpiresAt = token.ExpiresAt,
            },
            ct).ConfigureAwait(false);
    }

    private static VerifyIdentityResponse Failed(VerifyIdentityRequest req, string missing)
        => new()
        {
            Succeeded = false,
            Name = req.Name,
            Audience = req.Audience,
            Failure = $"Required value '{missing}' was not provided.",
        };

    private static string[] SplitScopes(string? scopes)
        => scopes is { Length: > 0 }
            ? scopes.Split([' '], StringSplitOptions.RemoveEmptyEntries)
            : [];
}
