using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Notifications.Abstractions;
using Fdw.Services.Notifications.Configuration;
using Fdw.Services.Notifications.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CmdBuilders = Fdw.Commands.Data.Extensions;

namespace Fdw.Services.Notifications.Services;

/// <summary>
/// DataGateway-backed user notification preference service over
/// <c>notify.UserNotificationPreference</c>.
/// </summary>
/// <remarks>
/// notify.UserNotificationPreference is plain application data inside the already-loaded
/// ConfigurationDb, so access goes through the standard <see cref="IDataGateway"/> path
/// (no bootstrap cycle), mirroring <c>SqlUserPreferenceService</c>.
/// </remarks>
public sealed class SqlUserNotificationPreferenceService : IUserNotificationPreferenceService
{
    private const string DataStoreName = "PlatformConfiguration";
    private const string PathName = "notify";
    private const string ContainerName = "UserNotificationPreference";
    private const string SystemActor = "system";

    private readonly IDataGatewayProvider _dataGateways;
    private readonly ILogger<SqlUserNotificationPreferenceService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlUserNotificationPreferenceService"/> class.
    /// </summary>
    public SqlUserNotificationPreferenceService(
        IDataGatewayProvider dataGateways,
        ILogger<SqlUserNotificationPreferenceService>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(dataGateways);

        _dataGateways = dataGateways;
        _logger = logger ?? NullLogger<SqlUserNotificationPreferenceService>.Instance;
    }

    // Why this throws rather than returning a result: every caller below is mid-query and has no
    // branch for "there is no gateway". The provider names the reason, which a Lazy could not.
    private IDataGateway Gateway => _dataGateways.ByName("Main");

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<NotificationPreference>>> GetPreferences(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        UserNotificationPreferenceLog.LoadingPreferences(_logger, userId);

        try
        {
            var current = await QueryCurrent(userId, cancellationToken).ConfigureAwait(false);
            if (!current.IsSuccess)
            {
                return GenericResult<IReadOnlyList<NotificationPreference>>.Failure(
                    UserNotificationPreferenceLog.QueryFailed(
                        _logger,
                        new InvalidOperationException(current.CurrentMessage ?? "Query failed"),
                        userId));
            }

            return GenericResult<IReadOnlyList<NotificationPreference>>.Success(Project(current.Value));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GenericResult<IReadOnlyList<NotificationPreference>>.Failure(
                UserNotificationPreferenceLog.QueryFailed(_logger, ex, userId));
        }
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<NotificationPreference>>> SavePreferences(
        Guid userId,
        IReadOnlyList<NotificationPreference> preferences,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        UserNotificationPreferenceLog.SavingPreferences(_logger, preferences.Count, userId);

        try
        {
            var existingResult = await QueryCurrent(userId, cancellationToken).ConfigureAwait(false);
            if (!existingResult.IsSuccess)
            {
                return GenericResult<IReadOnlyList<NotificationPreference>>.Failure(
                    UserNotificationPreferenceLog.QueryFailed(
                        _logger,
                        new InvalidOperationException(existingResult.CurrentMessage ?? "Query failed"),
                        userId));
            }

            var existing = (existingResult.Value ?? Enumerable.Empty<UserNotificationPreferenceConfiguration>())
                .ToDictionary(r => Key(r.NotificationType, r.Channel), StringComparer.OrdinalIgnoreCase);

            var now = DateTimeOffset.UtcNow;

            foreach (var preference in preferences)
            {
                if (existing.TryGetValue(Key(preference.NotificationType, preference.Channel), out var row))
                {
                    var updateCommand = CmdBuilders.Update.In<UserNotificationPreferenceConfiguration>(ContainerName)
                        .DataStore(DataStoreName).Path(PathName)
                        .Where(nameof(UserNotificationPreferenceConfiguration.UserId), row.UserId)
                        .Where(nameof(UserNotificationPreferenceConfiguration.NotificationType), row.NotificationType)
                        .Where(nameof(UserNotificationPreferenceConfiguration.Channel), row.Channel)
                        .Where(nameof(UserNotificationPreferenceConfiguration.IsCurrent), true)
                        .Value(new UserNotificationPreferenceConfiguration
                        {
                            IsEnabled = preference.IsEnabled,
                            ModifyDate = now,
                            ModifyBy = SystemActor,
                        });

                    var updateResult = await Gateway.Execute<int>(updateCommand, cancellationToken).ConfigureAwait(false);
                    if (!updateResult.IsSuccess)
                    {
                        return Fail(updateResult, preference, userId);
                    }
                }
                else
                {
                    var insertCommand = CmdBuilders.Insert.Into<UserNotificationPreferenceConfiguration>(ContainerName)
                        .DataStore(DataStoreName).Path(PathName)
                        .Value(new UserNotificationPreferenceConfiguration
                        {
                            UserId = userId,
                            NotificationType = preference.NotificationType,
                            Channel = preference.Channel,
                            IsEnabled = preference.IsEnabled,
                            IsCurrent = true,
                            IsDeleted = false,
                            CreateDate = now,
                            CreateBy = SystemActor,
                            ModifyDate = now,
                            ModifyBy = SystemActor,
                        });

                    var insertResult = await Gateway.Execute<int>(insertCommand, cancellationToken).ConfigureAwait(false);
                    if (!insertResult.IsSuccess)
                    {
                        return Fail(insertResult, preference, userId);
                    }
                }
            }

            UserNotificationPreferenceLog.PreferencesPersisted(_logger, preferences.Count, userId);

            return await GetPreferences(userId, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return GenericResult<IReadOnlyList<NotificationPreference>>.Failure(
                UserNotificationPreferenceLog.QueryFailed(_logger, ex, userId));
        }
    }

    private Task<IGenericResult<IEnumerable<UserNotificationPreferenceConfiguration>>> QueryCurrent(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var command = Query.From<UserNotificationPreferenceConfiguration>(DataStoreName, PathName, ContainerName)
            .Where(r => r.UserId).Equal(userId)
            .Where(r => r.IsCurrent).Equal(true)
            .Where(r => r.IsDeleted).Equal(false)
            .Build();

        return Gateway.Execute<IEnumerable<UserNotificationPreferenceConfiguration>>(command, cancellationToken);
    }

    private IGenericResult<IReadOnlyList<NotificationPreference>> Fail(
        IGenericResult writeResult,
        NotificationPreference preference,
        Guid userId)
    {
        return GenericResult<IReadOnlyList<NotificationPreference>>.Failure(
            UserNotificationPreferenceLog.SaveFailed(
                _logger,
                new InvalidOperationException(writeResult.CurrentMessage ?? "Write failed"),
                preference.NotificationType,
                preference.Channel,
                userId));
    }

    private static List<NotificationPreference> Project(
        IEnumerable<UserNotificationPreferenceConfiguration>? rows)
    {
        return (rows ?? Enumerable.Empty<UserNotificationPreferenceConfiguration>())
            .Select(r => new NotificationPreference
            {
                NotificationType = r.NotificationType,
                Channel = r.Channel,
                IsEnabled = r.IsEnabled,
            })
            .ToList();
    }

    private static string Key(string notificationType, string channel) => $"{notificationType}\0{channel}";
}
