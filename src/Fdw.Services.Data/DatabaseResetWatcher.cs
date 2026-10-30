using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Logging;
using Fdw.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Empties the result cache when a configuration database is reset, so reads stop being answered with rows that are
/// no longer there.
/// </summary>
/// <remarks>
/// <para>
/// The result cache is keyed by command and emptied by tag when a write goes through the same host. A database that is
/// dropped and created again by anything else (the database seed pipeline, <c>sqlpackage</c>, a script) touches no tag,
/// so a read made before the reset kept being served for the cache's whole life. Every so often this asks each
/// configuration gateway which generation of its database it reaches; a generation that differs from the last one seen,
/// or a database that cannot be reached, means what was cached may describe rows that no longer exist, and the cache is
/// emptied. When the database can be reached again that is a second change, and the cache is emptied again.
/// </para>
/// <para>
/// A connection of a kind that cannot tell which generation it reaches is left alone and said so once, not guessed at.
/// What this closes is the five minutes a cached read used to outlive a reset, to about one look; it does not make a
/// read in the instant of the reset exact. The first look at a database only records it: a cache emptied for a database
/// seen for the first time would be emptied for nothing.
/// </para>
/// </remarks>
public sealed class DatabaseResetWatcher : BackgroundService
{
    private readonly IDatabaseGenerationCatalog _catalog;
    private readonly ICacheInvalidator? _invalidator;
    private readonly TimeSpan? _interval;
    private readonly ILogger<DatabaseResetWatcher> _logger;

    private readonly Dictionary<string, Seen> _seen = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _cannotTell = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseResetWatcher"/> class.
    /// </summary>
    /// <param name="catalog">The configuration gateways the host has built.</param>
    /// <param name="invalidator">The result cache the gateways read through, or null when the host keeps none.</param>
    /// <param name="interval">How often the databases are looked at, or null when the host has no cache or no settings to say it, so nothing is watched.</param>
    /// <param name="logger">Optional logger.</param>
    public DatabaseResetWatcher(
        IDatabaseGenerationCatalog catalog,
        ICacheInvalidator? invalidator,
        TimeSpan? interval,
        ILogger<DatabaseResetWatcher>? logger = null)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        if (interval is { } given && given <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval), given, "An interval to look at the databases is positive.");

        _invalidator = invalidator;
        _interval = interval;
        _logger = logger ?? NullLogger<DatabaseResetWatcher>.Instance;
    }

    /// <summary>Gets how often the databases are looked at, or null when this watcher watches nothing.</summary>
    public TimeSpan? Interval => _interval;

    /// <summary>
    /// Builds the watcher the host starts with: the interval is the setting <see cref="SettingDefinitions.ResetWatchIntervalSeconds"/>, read once
    /// for the host under the system context, since no request is in flight when the host starts.
    /// </summary>
    /// <param name="services">The host's services.</param>
    /// <returns>The watcher; one that watches nothing when the host keeps no result cache, or registers no settings to read the interval from.</returns>
    /// <exception cref="InvalidOperationException">The host has settings and the setting is missing or not positive; the host does not start without it.</exception>
    public static DatabaseResetWatcher Create(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger<DatabaseResetWatcher>();
        var catalog = services.GetRequiredService<IDatabaseGenerationCatalog>();
        var invalidator = services.GetService<ICacheInvalidator>();
        if (invalidator is null)
            return new DatabaseResetWatcher(catalog, null, null, logger);

        if (services.GetService<IEffectiveSettingsProvider>() is not { } settings)
        {
            DatabaseResetWatcherLog.NoIntervalSource(logger, SettingDefinitions.ResetWatchIntervalSeconds);
            return new DatabaseResetWatcher(catalog, invalidator, null, logger);
        }

        using var scope = services.CreateScope();
        using var system = new SystemAuthenticationContextScope(scope.ServiceProvider.GetRequiredService<IAuthenticationContextAccessor>());
        var interval = DatabaseResetWatchInterval.Read(settings, logger);
        return interval.IsSuccess
            ? new DatabaseResetWatcher(catalog, invalidator, interval.Value, logger)
            : throw new InvalidOperationException(string.Concat(interval.CurrentMessage));
    }

    /// <summary>
    /// Looks once at every configuration database the host has built a gateway for, and empties the result cache when
    /// any of them was reset or cannot be reached.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> when the cache was emptied by this look.</returns>
    public async Task<bool> Observe(CancellationToken cancellationToken = default)
    {
        var changed = false;
        foreach (var source in _catalog.Sources())
        {
            if (Judge(source.ConnectionName, await Ask(source, cancellationToken).ConfigureAwait(false)))
                changed = true;
        }

        if (changed)
            _invalidator?.InvalidateAll();

        return changed;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_invalidator is null)
        {
            DatabaseResetWatcherLog.NoCache(_logger);
            return;
        }

        if (_interval is not { } interval)
            return;

        DatabaseResetWatcherLog.Started(_logger, interval);
        using var timer = new PeriodicTimer(interval);
        try
        {
            // The first look is at once, so the baseline is taken before the cache has been filled from a database that is then reset.
            await Observe(stoppingToken).ConfigureAwait(false);
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
                await Observe(stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            DatabaseResetWatcherLog.Stopped(_logger);
        }
    }

    // The answer of one gateway; a throw is logged and returned as an unreachable database, so one gateway cannot stop the look at the others.
    private async Task<IGenericResult<DatabaseGenerationReading>> Ask(IDatabaseGenerationSource source, CancellationToken cancellationToken)
    {
        try
        {
            return await source.DatabaseGeneration(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return GenericResult<DatabaseGenerationReading>.Failure(
                DatabaseResetWatcherLog.AskFailed(_logger, ex, source.ConnectionName));
        }
    }

    // Compares what a database reports now with what it reported last, records it, and says whether the cache has to be emptied.
    private bool Judge(string connection, IGenericResult<DatabaseGenerationReading> reading)
    {
        if (reading.IsSuccess && reading.Value is { Tells: false })
        {
            if (_cannotTell.Add(connection))
                DatabaseResetWatcherLog.CannotTell(_logger, connection);

            return false;
        }

        var now = reading.IsSuccess && reading.Value is { } told ? Seen.At(told.Generation) : Seen.Unreachable;
        if (!_seen.TryGetValue(connection, out var before))
        {
            _seen[connection] = now;
            DatabaseResetWatcherLog.FirstLook(_logger, connection, now.Reachable ? now.Generation : "not reachable");
            return false;
        }

        _seen[connection] = now;
        if (now.Equals(before))
            return false;

        if (!now.Reachable)
            DatabaseResetWatcherLog.Unreachable(_logger, connection, reading.CurrentMessage ?? "no reason was given");
        else if (!before.Reachable)
            DatabaseResetWatcherLog.Reachable(_logger, connection, now.Generation);
        else
            DatabaseResetWatcherLog.Reset(_logger, connection, before.Generation, now.Generation);

        return true;
    }

    // What one look found: a generation, or that the database could not be reached.
    private sealed record Seen(bool Reachable, string Generation)
    {
        public static Seen Unreachable { get; } = new(false, string.Empty);

        public static Seen At(string generation) => new(true, generation);
    }
}
