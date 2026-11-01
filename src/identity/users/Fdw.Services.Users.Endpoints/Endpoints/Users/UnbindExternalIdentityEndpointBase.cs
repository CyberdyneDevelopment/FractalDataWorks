using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.Services.Users.Configuration;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Users.Endpoints;

/// <summary>
/// Generic base endpoint removing an external-identity binding from an account.
/// </summary>
/// <remarks>
/// Removing the binding does not disable the account: the next sign-in through that provider is
/// simply unbound again, and whether it provisions a new account is the provisioner's decision.
/// </remarks>
public abstract class UnbindExternalIdentityEndpointBase : Endpoint<UnbindExternalIdentityRequest>
{
    private const string ExternalIdentityDomain = "ExternalIdentity";

    private readonly UserConfigurationProvider _userProvider;
    private readonly IExternalIdentityConfigurationProvider _externalIdentities;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="UnbindExternalIdentityEndpointBase"/> class.</summary>
    protected UnbindExternalIdentityEndpointBase(
        ILogger logger,
        UserConfigurationProvider userProvider,
        IExternalIdentityConfigurationProvider externalIdentities)
    {
        _logger = logger;
        _userProvider = userProvider;
        _externalIdentities = externalIdentities;
    }

    /// <summary>Gets the RBAC policy required by this endpoint. Defaults to "users:write".</summary>
    protected virtual string WritePolicy => "users:write";

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/users/{IdOrName}/external-identities/{BindingId}");
        Policies(WritePolicy);
        Summary(s =>
        {
            s.Summary = "Remove an external-identity binding";
            s.Description = "Unbinds a provider's subject from this account.";
        });
        ConfigureEndpoint();
    }

    /// <summary>Override to add tags or further configuration.</summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc />
    public override async Task HandleAsync(UnbindExternalIdentityRequest req, CancellationToken ct)
    {
        var userResult = await UserRouteTarget
            .Resolve(_userProvider, User, req.IdOrName, ct).ConfigureAwait(false);

        if (!userResult.IsSuccess || userResult.Value is null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var user = userResult.Value;

        if (!AccountAuthority.Administers(User, user, Resolve<ISystemRoleConfiguration>()))
        {
            UserEndpointLog.ApiKeyOperationNotPermitted(_logger, req.IdOrName);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        // The binding is read back before it is removed: a binding id that belongs to another
        // account is a not-found here, never a delete of someone else's row.
        var bindings = await _externalIdentities
            .Find<ExternalIdentityConfiguration>(b => b.UserId == user.Id && b.Id == req.BindingId, ct)
            .ConfigureAwait(false);
        if (bindings.IsFailure || bindings.Value is null || !bindings.Value.Any())
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var deleteResult = await _externalIdentities
            .Delete(bindings.Value[0].Id, ct).ConfigureAwait(false);
        if (!deleteResult.IsSuccess)
        {
            AddError("The binding could not be removed.");
            await Send.ErrorsAsync(400, ct).ConfigureAwait(false);
            return;
        }

        UserEndpointLog.ExternalIdentityUnbound(_logger, req.IdOrName);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
