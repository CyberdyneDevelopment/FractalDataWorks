using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Authorization.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Steps;

/// <summary>
/// Resolves what the principal may do and states it on the context, for the token to carry.
/// </summary>
/// <remarks>
/// <para>
/// Separate from <see cref="AuthorizeIssuanceStep"/> deliberately: that step answers whether a token
/// may be issued at all, this one gathers what the token asserts. Folding them together would make
/// one step contribute both a decision and a claim set, and the reason to refuse issuance has
/// nothing to do with the permissions a permitted caller turns out to hold.
/// </para>
/// <para>
/// Baking at issuance rather than resolving per request is what lets a resource server authorize
/// from the token alone. The cost is that a permission change does not reach a token already
/// minted — it takes effect when the next one is, which is what the token's lifetime bounds.
/// </para>
/// </remarks>
public sealed class BakePermissionsStep : IAuthenticationStep
{
    private readonly IEffectivePermissionResolver _permissions;
    private readonly ILogger<BakePermissionsStep> _logger;

    /// <summary>Initializes a new instance of the <see cref="BakePermissionsStep"/> class.</summary>
    /// <param name="permissions">Resolves a principal's effective permissions.</param>
    /// <param name="logger">The logger.</param>
    public BakePermissionsStep(
        IEffectivePermissionResolver permissions,
        ILogger<BakePermissionsStep>? logger = null)
    {
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _logger = logger ?? NullLogger<BakePermissionsStep>.Instance;
    }

    /// <inheritdoc />
    public IReadOnlyList<ContextElement> Requires => [ContextElement.Principal];

    /// <inheritdoc />
    public IReadOnlyList<ContextElement> Contributes => [ContextElement.Claims];

    /// <inheritdoc />
    /// <remarks>Gathering entitlements proves nothing about who the caller is.</remarks>
    public IReadOnlyList<string> AuthenticationMethods => [];

    /// <inheritdoc />
    public async Task<IGenericResult<StepOutcome>> Execute(
        AuthenticationContext context, CancellationToken cancellationToken = default)
    {
        var principal = context.Principal!;

        var resolved = await _permissions
            .Resolve(
                principal.Id.ToString(),
                principal.TenantId,
                orgId: null,
                isGlobalTenant: false,
                cancellationToken)
            .ConfigureAwait(false);

        if (resolved.IsFailure)
            return resolved.ToNewResult<StepOutcome>();

        var permissions = resolved.Value ?? [];

        // Why Local and not Derived: these are read from this platform's own authorization tables.
        // Derived is for what the runner works out itself, such as an assurance level.
        var claims = permissions
            .Select(permission => new Claim
            {
                Type = ClaimDefinitions.perm.Name,
                Value = permission,
                Source = ClaimSource.Local,
            })
            .ToList();

        PermissionBakingLog.Baked(_logger, principal.Id, claims.Count);

        return GenericResult<StepOutcome>.Success(
            new StepOutcome.Contributed(new ContextContribution { Claims = claims }));
    }
}
