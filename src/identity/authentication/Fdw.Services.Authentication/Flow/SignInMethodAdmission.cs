using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Fdw.Services.Authorization.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Flow;

/// <summary>
/// Answers whether a user could sign in by a method, from the enabled flows' stated requirements.
/// </summary>
/// <remarks>
/// Permissions are resolved in the tenant the user's sign-in lands in (their default membership), the
/// same tenant an API key acts in. That is the tenant a gated step sees the baked permissions of.
/// </remarks>
public sealed class SignInMethodAdmission : ISignInMethodAdmission
{
    private readonly IAuthenticationFlowProvider _flows;
    private readonly ITenantResolver _tenants;
    private readonly IUserPermissions _permissions;
    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly Func<string, IAuthenticationStep?> _step;
    private readonly ILogger<SignInMethodAdmission> _logger;

    /// <summary>Initializes a new instance of the <see cref="SignInMethodAdmission"/> class.</summary>
    /// <param name="flows">Supplies the enabled, valid flows.</param>
    /// <param name="tenants">Supplies the tenant a user's sign-in lands in.</param>
    /// <param name="permissions">Resolves what the user holds there.</param>
    /// <param name="authContextAccessor">Establishes the trusted context for the permission read.</param>
    /// <param name="step">How a name becomes a step, to read which methods a flow proves.</param>
    /// <param name="logger">The logger.</param>
    public SignInMethodAdmission(
        IAuthenticationFlowProvider flows,
        ITenantResolver tenants,
        IUserPermissions permissions,
        IAuthenticationContextAccessor authContextAccessor,
        Func<string, IAuthenticationStep?> step,
        ILogger<SignInMethodAdmission>? logger = null)
    {
        _flows = flows ?? throw new ArgumentNullException(nameof(flows));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _authContextAccessor = authContextAccessor ?? throw new ArgumentNullException(nameof(authContextAccessor));
        _step = step ?? throw new ArgumentNullException(nameof(step));
        _logger = logger ?? NullLogger<SignInMethodAdmission>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<bool>> MayUse(
        Guid userId, string authenticationMethod, CancellationToken cancellationToken = default)
    {
        var proving = await Proving(authenticationMethod, cancellationToken).ConfigureAwait(false);
        if (proving.IsFailure)
            return proving.ToNewResult<bool>();

        // Resolved only when a gated flow needs it: an open flow answers without a permission read.
        if (proving.Value!.Count == 0 || proving.Value.Exists(f => f.RequiredPermissions.Count == 0))
            return GenericResult<bool>.Success(Decide(proving.Value, new HashSet<string>(StringComparer.OrdinalIgnoreCase), authenticationMethod, userId.ToString()));

        var held = await Held(userId, cancellationToken).ConfigureAwait(false);
        if (held.IsFailure)
            return held.ToNewResult<bool>();

        return GenericResult<bool>.Success(Decide(proving.Value, held.Value!, authenticationMethod, userId.ToString()));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<bool>> MayUse(
        IReadOnlyCollection<string> heldPermissions, string authenticationMethod, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(heldPermissions);

        var proving = await Proving(authenticationMethod, cancellationToken).ConfigureAwait(false);
        if (proving.IsFailure)
            return proving.ToNewResult<bool>();

        return GenericResult<bool>.Success(Decide(
            proving.Value!, new HashSet<string>(heldPermissions, StringComparer.OrdinalIgnoreCase),
            authenticationMethod, "an account not yet created"));
    }

    // The enabled flows whose steps prove the method.
    private async Task<IGenericResult<List<AuthenticationFlow>>> Proving(
        string authenticationMethod, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(authenticationMethod))
            return GenericResult<List<AuthenticationFlow>>.Failure(SignInAdmissionLog.MethodMissing(_logger));

        var flows = await _flows.Get(cancellationToken).ConfigureAwait(false);
        if (flows.IsFailure)
            return flows.ToNewResult<List<AuthenticationFlow>>();

        return GenericResult<List<AuthenticationFlow>>.Success((flows.Value ?? [])
            .Where(f => f.Steps.Any(s => _step(s.Name) is { } step
                && step.AuthenticationMethods.Contains(authenticationMethod, StringComparer.Ordinal)))
            .ToList());
    }

    // One flow proving the method admits the principal: an open one admits everyone, a gated one those
    // holding all it requires. No flow proving the method admits nobody.
    private bool Decide(List<AuthenticationFlow> proving, ISet<string> held, string authenticationMethod, string who)
    {
        if (proving.Count == 0)
        {
            SignInAdmissionLog.NoFlowProvesMethod(_logger, authenticationMethod);
            return false;
        }

        if (proving.Find(f => f.RequiredPermissions.Count == 0) is { } open)
        {
            SignInAdmissionLog.AdmittedByOpenFlow(_logger, who, authenticationMethod, open.Name);
            return true;
        }

        if (proving.Find(f => f.RequiredPermissions.All(held.Contains)) is { } admitting)
        {
            SignInAdmissionLog.AdmittedByGatedFlow(_logger, who, authenticationMethod, admitting.Name);
            return true;
        }

        SignInAdmissionLog.NotAdmitted(_logger, who, authenticationMethod,
            string.Join(", ", proving.Select(f => f.Name + " [" + string.Join(", ", f.RequiredPermissions) + "]")));
        return false;
    }

    private async Task<IGenericResult<HashSet<string>>> Held(Guid userId, CancellationToken cancellationToken)
    {
        // The caller is acting on somebody else's account, whose roles live in a tenant the caller
        // may not see; this read is the platform's own authentication question, as at sign-in.
        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);

        var tenant = await _tenants.TenantFor(userId, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
            return tenant.ToNewResult<HashSet<string>>();

        var resolved = await _permissions
            .Get(userId.ToString(), tenant.Value, orgId: null, cancellationToken)
            .ConfigureAwait(false);
        if (resolved.IsFailure)
            return resolved.ToNewResult<HashSet<string>>();
        if (resolved.Value is not { } authorization)
            return GenericResult<HashSet<string>>.Failure(SignInAdmissionLog.PermissionsUnresolved(_logger, userId));

        return GenericResult<HashSet<string>>.Success(
            new HashSet<string>(authorization.Permissions, StringComparer.OrdinalIgnoreCase));
    }
}
