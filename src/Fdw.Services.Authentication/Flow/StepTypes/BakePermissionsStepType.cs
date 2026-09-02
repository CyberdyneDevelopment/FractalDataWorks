using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Authorization.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Flow.StepTypes;

/// <summary>
/// Resolves what the principal may do and states it on the context, for the token to carry.
/// </summary>
/// <remarks>
/// The option IS the step: a flow names it, the collection answers by that name, and what answers
/// is the thing that runs.
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(AuthenticationStepTypes), "BakePermissions")]
public sealed class BakePermissionsStepType
    : AuthenticationStepTypeBase<IGenericService, IServiceFactory<IGenericService, IServiceConfiguration>>,
      IAuthenticationStep
{
    // Captured when the host is built: an option is created by its module initializer, which needs
    // a parameterless constructor, so what it needs arrives where a live container exists.
    private IEffectivePermissionResolver? _permissions;
    private ILogger _logger = NullLogger<BakePermissionsStepType>.Instance;

    /// <summary>Initializes a new instance of the <see cref="BakePermissionsStepType"/> class.</summary>
    public BakePermissionsStepType()
        : base("BakePermissions",
               "AuthenticationSteps",
               "Bake Permissions",
               "Resolves the principal's effective permissions and states them for the token to carry")
    {
        Initialization((host, loggerFactory) =>
        {
            _permissions = host.Services.GetRequiredService<IEffectivePermissionResolver>();
            _logger = loggerFactory?.CreateLogger<BakePermissionsStepType>()
                ?? NullLogger<BakePermissionsStepType>.Instance;

            return GenericResult<IHost>.Success(host);
        });
    }

    /// <inheritdoc />
    public IReadOnlyList<IContextElement> Requires => [ContextElements.Principal];

    /// <inheritdoc />
    public IReadOnlyList<IContextElement> Contributes => [ContextElements.Claims];

    /// <inheritdoc />
    /// <remarks>Gathering entitlements proves nothing about who the caller is.</remarks>
    public IReadOnlyList<string> AuthenticationMethods => [];

    /// <inheritdoc />
    public async Task<IGenericResult<StepOutcome>> Execute(
        AuthenticationContext context, CancellationToken cancellationToken = default)
    {
        // Baking nothing would hand out a token carrying no permissions, which reads downstream as
        // a caller who may do nothing rather than as a step that never ran.
        if (_permissions is null)
            return GenericResult<StepOutcome>.Failure(PermissionBakingLog.NotInitialized(_logger, Name));

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
                Source = ClaimSources.Local,
            })
            .ToList();

        PermissionBakingLog.Baked(_logger, principal.Id, claims.Count);

        return GenericResult<StepOutcome>.Success(
            new StepOutcome.Contributed(new ContextContribution { Claims = claims }));
    }
}
