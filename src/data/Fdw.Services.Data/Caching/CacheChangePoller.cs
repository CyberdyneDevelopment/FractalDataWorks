using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Fdw.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Caching;

/// <summary>
/// Drops from this host's result cache the configuration other hosts changed: every so often it reads the cache change
/// tokens, and a tag whose token is not the one it saw last is invalidated (FDW-4782).
/// </summary>
/// <remarks>
/// <para>
/// A write in one host evicts that host's cache at once (the gateway does it) and publishes the tags it changed through
/// <see cref="CacheChangePublisher"/>; this is the other half, in every host that keeps a cache. A change in another host
/// is dropped here within one interval, instead of being served until the entry expires.
/// </para>
/// <para>
/// The first look only records the tokens: a cache dropped for tokens seen for the first time would be dropped for
/// nothing. After it, a tag seen for the first time is a first change and is dropped too. Tokens are compared for
/// equality only, so two writes are never ordered. The tokens are read past the cache, under the system context, since
/// a token row belongs to no tenant and a background thread has no caller. A database that cannot be reached is
/// <see cref="DatabaseResetWatcher"/>'s, which empties the whole cache; a failed read here is logged and tried at the
/// next look.
/// </para>
/// </remarks>
public sealed class CacheChangePoller : BackgroundService
{
    private readonly Func<IGenericResult<IConfigurationGateway>> _gateway;
    private readonly IAuthenticationContextAccessor _authenticationContextAccessor;
    private readonly ICacheInvalidator? _invalidator;
    private readonly DataStoreTarget _target;
    private readonly TimeSpan? _interval;
    private readonly ILogger<CacheChangePoller> _logger;

    private Dictionary<string, Guid>? _seen;

    /// <summary>Initializes a new instance of the <see cref="CacheChangePoller"/> class.</summary>
    /// <param name="gateway">Gives the gateway onto the store the tokens live in, asked at each look.</param>
    /// <param name="authenticationContextAccessor">The accessor the system context is set on for the read.</param>
    /// <param name="invalidator">The host's result cache, or null when the host keeps none, so nothing is looked at.</param>
    /// <param name="target">The container the tokens live in.</param>
    /// <param name="interval">How often to look; null only when the host keeps no cache.</param>
    /// <param name="logger">Optional logger.</param>
    public CacheChangePoller(
        Func<IGenericResult<IConfigurationGateway>> gateway,
        IAuthenticationContextAccessor authenticationContextAccessor,
        ICacheInvalidator? invalidator,
        DataStoreTarget target,
        TimeSpan? interval,
        ILogger<CacheChangePoller>? logger = null)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _authenticationContextAccessor = authenticationContextAccessor ?? throw new ArgumentNullException(nameof(authenticationContextAccessor));
        _target = target ?? throw new ArgumentNullException(nameof(target));

        if (invalidator is not null && interval is null)
            throw new ArgumentNullException(nameof(interval), "A host that keeps a cache states how often the published changes are looked at.");

        if (interval is { } given && given <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval), given, "An interval to look at the published changes is positive.");

        _invalidator = invalidator;
        _interval = interval;
        _logger = logger ?? NullLogger<CacheChangePoller>.Instance;
    }

    /// <summary>Gets how often the published changes are looked at, or null when the host keeps no cache.</summary>
    public TimeSpan? Interval => _interval;

    /// <summary>
    /// Builds the poller the host starts with. A host that keeps no result cache (no cache registered, or
    /// <see cref="MainDataGatewayConfiguration.EnableCache"/> off) gets one that looks at nothing. A host that keeps one looks
    /// as often as the setting <see cref="SettingDefinitions.CacheChangePollIntervalSeconds"/> states, read once under the
    /// system context since no request is in flight when the host starts, or as
    /// <see cref="ConfigurationGatewayTypes.CacheChangePollInterval"/> declares when it states nothing.
    /// </summary>
    /// <param name="services">The host's services.</param>
    /// <returns>The poller.</returns>
    /// <exception cref="InvalidOperationException">The setting states a number that is not positive; the host does not start.</exception>
    public static CacheChangePoller Create(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<CacheChangePoller>();
        var accessor = services.GetRequiredService<IAuthenticationContextAccessor>();
        IGenericResult<IConfigurationGateway> Gateway()
            => services.GetRequiredService<IConfigurationGatewayProvider>().Get(ConfigurationGatewayTypes.ConfigurationConnection);

        if (!ConfigurationGatewayTypes.PublishesCacheChanges)
        {
            CacheChangeLog.PlatformIsServerTier(logger, ConfigurationGatewayTypes.ConfigurationConnection);
            return new CacheChangePoller(Gateway, accessor, null, CacheChangeTokens.Target, null, logger);
        }

        var invalidator = services.GetService<ICacheInvalidator>();
        if (invalidator is null || services.GetService<MainDataGatewayConfiguration>() is not { EnableCache: true })
            return new CacheChangePoller(Gateway, accessor, null, CacheChangeTokens.Target, null, logger);

        using var scope = services.CreateScope();
        using var system = new SystemAuthenticationContextScope(scope.ServiceProvider.GetRequiredService<IAuthenticationContextAccessor>());
        var interval = CacheChangePollInterval.Read(
            services.GetService<IEffectiveSettingsProvider>(), ConfigurationGatewayTypes.CacheChangePollInterval, logger);
        return interval.IsSuccess
            ? new CacheChangePoller(Gateway, accessor, invalidator, CacheChangeTokens.Target, interval.Value, logger)
            : throw new InvalidOperationException(string.Concat(interval.CurrentMessage));
    }

    /// <summary>
    /// Looks once at the published changes and drops from the cache every tag whose token changed since the last look.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tags dropped by this look; none on the first look, or when the tokens could not be read.</returns>
    public async Task<IReadOnlyList<string>> Observe(CancellationToken cancellationToken = default)
    {
        var tokens = await Read(cancellationToken).ConfigureAwait(false);
        if (tokens is null)
            return [];

        var now = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in tokens)
            now[token.Tag] = token.Token;

        if (_seen is null)
        {
            _seen = now;
            CacheChangeLog.FirstLook(_logger, now.Count);
            return [];
        }

        var changed = now
            .Where(token => !_seen.TryGetValue(token.Key, out var before) || before != token.Value)
            .Select(token => token.Key)
            .ToList();

        _seen = now;
        if (changed.Count == 0 || _invalidator is null)
            return changed;

        _invalidator.InvalidateByTags(changed);
        CacheChangeLog.ChangesApplied(_logger, changed.Count, string.Join(", ", changed));
        return changed;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_invalidator is null || _interval is not { } interval)
        {
            CacheChangeLog.NoCache(_logger);
            return;
        }

        CacheChangeLog.PollerStarted(_logger, interval);
        using var timer = new PeriodicTimer(interval);
        try
        {
            // The first look is at once, so the baseline is taken before the cache fills.
            _ = await Observe(stoppingToken).ConfigureAwait(false);
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                _ = await Observe(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            CacheChangeLog.PollerStopped(_logger);
        }
    }

    // The tokens, or null when they could not be read (logged).
    private async Task<IReadOnlyList<CacheChangeToken>?> Read(CancellationToken cancellationToken)
    {
        try
        {
            var gateway = _gateway();
            if (!gateway.IsSuccess || gateway.Value is not { } store)
            {
                CacheChangeLog.ReadFailed(_logger, gateway.NoValueReason().Message);
                return null;
            }

            using var system = new SystemAuthenticationContextScope(_authenticationContextAccessor);
            var read = await store.Execute<IEnumerable<CacheChangeToken>>(
                new QueryCommand<CacheChangeToken>(), _target, useCache: false, cancellationToken).ConfigureAwait(false);
            if (!read.IsSuccess)
            {
                CacheChangeLog.ReadFailed(_logger, read.FailureReason().Message);
                return null;
            }

            return [.. read.Value ?? []];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            CacheChangeLog.ReadThrew(_logger, ex);
            return null;
        }
    }
}
