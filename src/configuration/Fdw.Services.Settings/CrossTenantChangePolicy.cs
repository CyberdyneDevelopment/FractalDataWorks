using System;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Settings;

/// <summary>
/// Reads the cross-tenant change setting and decides (FDW-4459).
/// </summary>
/// <remarks>
/// The order is the setting row (the server's), then the value <see cref="CrossTenantChangeOption"/> declares or a host overrides, then a coded failure. A change inside the
/// caller's own tenant never reads a setting.
/// </remarks>
public sealed class CrossTenantChangePolicy : ICrossTenantChangePolicy
{
    private readonly IEffectiveSettingsProvider _settings;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="CrossTenantChangePolicy"/> class.</summary>
    /// <param name="settings">The settings provider.</param>
    /// <param name="logger">The logger.</param>
    public CrossTenantChangePolicy(IEffectiveSettingsProvider settings, ILogger<CrossTenantChangePolicy>? logger = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? NullLogger<CrossTenantChangePolicy>.Instance;
    }

    /// <inheritdoc/>
    public IGenericResult Permit(string changeName, Guid ownerTenantId, Guid? actorTenantId)
    {
        if (actorTenantId is not { } actor)
        {
            return GenericResult.Failure(CrossTenantChangeLog.ActorTenantMissing(_logger, changeName, ownerTenantId));
        }

        if (actor == ownerTenantId)
        {
            CrossTenantChangeLog.Allowed(_logger, changeName, ownerTenantId, actor, "same tenant");
            return GenericResult.Success();
        }

        var allowed = Resolve(changeName);
        if (allowed.IsFailure)
        {
            return allowed;
        }

        if (!allowed.Value)
        {
            return GenericResult.Failure(CrossTenantChangeLog.Refused(_logger, changeName, ownerTenantId, actor));
        }

        CrossTenantChangeLog.Allowed(_logger, changeName, ownerTenantId, actor, "the setting allows it");
        return GenericResult.Success();
    }

    private IGenericResult<bool> Resolve(string changeName)
    {
        if (_settings.TryGetEffectiveValue<bool>(changeName, out var stated))
        {
            return GenericResult<bool>.Success(stated);
        }

        return ServiceTypeDefaults.TryResolve(CrossTenantChangeOption.Instance, changeName, out var declared) && declared is bool answer
            ? GenericResult<bool>.Success(answer)
            : GenericResult<bool>.Failure(CrossTenantChangeLog.SettingNotDeclared(_logger, changeName));
    }
}
