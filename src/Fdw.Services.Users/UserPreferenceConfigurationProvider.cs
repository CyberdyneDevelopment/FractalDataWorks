using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Configuration;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Users.Commands;
using Fdw.Services.Users.Logging;
using Fdw.Services.Users.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Fdw.Services.Users;

/// <summary>
/// Domain configuration provider for user preferences. Sole owner of <c>usr.UserPreferences</c> gatewayProvider access.
/// Thin wrapper over <see cref="ImplementationConfigurationProviderBase{TConfig,TCommand}"/> with a by-userId query.
/// </summary>
/// <remarks>
/// All reads and writes go through <see cref="IConfigurationGateway"/>. No <see cref="Fdw.Services.Data.Abstractions.IDataGateway"/>
/// usage — usr.UserPreferences is ConfigurationDb data accessed through the config gatewayProvider, same as usr.Users.
/// </remarks>
public class UserPreferenceConfigurationProvider
    : ImplementationConfigurationProviderBase<UserPreferencesConfiguration, UserPreferenceConfigurationCommand>
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="UserPreferenceConfigurationProvider"/> class.</summary>
    public UserPreferenceConfigurationProvider(
        ILogger<UserPreferenceConfigurationProvider>? logger,
        IConfigurationGatewayProvider gatewayProvider,
        string dataStoreName = "ConfigurationDb",
        string pathName = "usr")
        : base(logger ?? NullLogger<UserPreferenceConfigurationProvider>.Instance,
               gatewayProvider,
               dataStoreName, pathName)
    {
        _logger = logger ?? NullLogger<UserPreferenceConfigurationProvider>.Instance;
    }

    /// <summary>
    /// Gets the preferences for the specified user (returns null when none exist yet).
    /// </summary>
    // Why: virtual allows Moq to override in unit tests without a real IOptionsMonitor or gateway.
    public virtual async Task<IGenericResult<UserPreferencesConfiguration?>> GetPreferences(
        Guid userId, CancellationToken cancellationToken = default)
    {
        UserConfigurationProviderLog.LoadPreferencesTrace(_logger, userId);

        var command = new QueryCommandBuilder<UserPreferencesConfiguration>(
                DataStoreName, PathName, "UserPreferences")
            .Where(nameof(UserPreferencesConfiguration.UserId), userId)
            .Where(nameof(UserPreferencesConfiguration.IsCurrent), true)
            .Where(nameof(UserPreferencesConfiguration.IsDeleted), false)
            .Build();

        var result = await Execute<IEnumerable<UserPreferencesConfiguration>>(command, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            UserConfigurationProviderLog.LoadPreferencesFailed(_logger, userId);
            return result.Messages.Any()
                ? result.ToNewResult<UserPreferencesConfiguration?>()
                : GenericResult<UserPreferencesConfiguration?>.Failure(UserConfigurationProviderLog.LoadPreferencesFailed(_logger, userId));
        }

        return GenericResult<UserPreferencesConfiguration?>.Success(result.Value?.FirstOrDefault());
    }
}
