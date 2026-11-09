using System;
using Fdw.Results;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Fdw.ServiceTypes.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.Abstractions;

namespace Fdw.ServiceTypes;

/// <summary>
/// Opt-in, process-global registry and aggregate three-phase entry point for every discovered
/// ServiceTypeCollection. Populated by the <c>[ModuleInitializer]</c> emitted by
/// <c>Fdw.Services.Registration.SourceGenerators</c> when the entry-point assembly loads — an app
/// gets this behavior only if it references that generator; otherwise this type is simply absent
/// from its dependency graph.
/// </summary>
/// <remarks>
/// <para>
/// No name-based lookup surface (no <c>ByName</c>) — each discovered domain gets its own generated,
/// dot-walkable property directly on this class (e.g. <c>PlatformServices.Connection</c>), backed by a
/// private field the generator assigns from <see cref="Add"/>'s return value. There is nothing to look
/// up at read time, so there is no dictionary here at all — just the pending/frozen entry lists.
/// </para>
/// <para>
/// NO FALLBACKS WITHOUT EXPLICIT APPROVAL. <see cref="Add"/> fails loud (throws) rather than silently
/// accepting an inconsistent state.
/// </para>
/// </remarks>
public static class PlatformServices
{
    private static readonly object _gate = new();

    /// <summary>
    /// Whether a singleton that takes a scoped or a transient service stops the host at Register (FDW-4640). A host that needs to start while it clears the findings sets this to
    /// <c>false</c> before <see cref="Register"/>; every finding is still logged, at Warning in the summary.
    /// </summary>
    /// <remarks>
    /// Why a setting and not a switch in code: Release-Supervisor's ruling for the first window, so a host can show all its findings without stopping at the first. It governs the
    /// registration walk only; ValidateScopes and ValidateOnBuild stay on, because a scoped service resolved from the root provider cannot be tolerated at run time.
    /// </remarks>
    public static bool LifetimeFindingsFailStartup { get; set; } = true;

    // Declared before the setting that starts from it: static initializers run in the order written.
    private static readonly Type[] DefaultLifetimeCheckSkippedServiceTypes = [typeof(IOptionsFactory<>)];

    /// <summary>
    /// The service types the registration lifetime check does not report as a singleton's dependency (FDW-4640). An open generic type names every closed form of it.
    /// </summary>
    /// <remarks>
    /// Why a list the host can change: Microsoft.Extensions.Options registers IOptions&lt;T&gt; and IOptionsMonitor&lt;T&gt; as singletons that take the transient
    /// IOptionsFactory&lt;T&gt;, which is the framework's own design and not a captive dependency of ours. The default lists that one type; a host that finds another framework
    /// registration of the same kind sets the list before <see cref="Register"/> rather than the check naming types in code.
    /// </remarks>
    public static IReadOnlyCollection<Type> LifetimeCheckSkippedServiceTypes { get; set; } = DefaultLifetimeCheckSkippedServiceTypes;


    // Pre-freeze staging. Written only by [ModuleInitializer] Add() calls, which are CLR-guaranteed
    // complete before Main() and thus before anything can trigger the freeze.
    private static readonly List<PlatformServiceEntry> _pending = new();

    // Frozen snapshot — built exactly once, lazily, inside EnsureFrozen().
    private static ImmutableArray<PlatformServiceEntry> _frozenOrder;
    private static volatile bool _frozen;

    /// <summary>
    /// Registers a ServiceTypeCollection descriptor under <paramref name="categoryName"/>, returning
    /// the created (or, for a harmless duplicate re-registration, the existing) entry. Called exclusively from the source-generated
    /// <c>[ModuleInitializer]</c> as the entry-point assembly loads — one call per discovered
    /// <c>[ServiceTypeCollection]</c> domain. The generator assigns the return value to that domain's
    /// own private field, which its generated dot-walkable property reads from directly — no lookup of
    /// any kind happens at read time.
    /// </summary>
    /// <param name="categoryName">Category name (e.g. "Connection") — matches the collection's <c>ServiceCategory</c>.</param>
    /// <param name="serviceCollection">Descriptor exposing the collection's three-phase entry points.</param>
    /// <exception cref="InvalidOperationException">
    /// The registry is already frozen, or <paramref name="categoryName"/> is already registered to a
    /// <em>different</em> collection type (a real conflict — never silently last-write-wins).
    /// </exception>
    public static PlatformServiceEntry Add(string categoryName, IServiceTypeCollection serviceCollection)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            throw new ArgumentException("categoryName must not be empty.", nameof(categoryName));
        if (serviceCollection is null)
            throw new ArgumentNullException(nameof(serviceCollection));

        lock (_gate)
        {
            if (_frozen)
                throw new InvalidOperationException(
                    $"Cannot add '{categoryName}': PlatformServices is already frozen. Add() may only " +
                    "run from a [ModuleInitializer], which is guaranteed complete before Main() and thus " +
                    "before the first Configure/Register/Initialize/Entries call freezes the registry.");

            var existing = _pending.FirstOrDefault(
                e => string.Equals(e.CategoryName, categoryName, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                if (!ReferenceEquals(existing.Descriptor.CollectionType, serviceCollection.CollectionType))
                    throw new InvalidOperationException(
                        $"Category '{categoryName}' is already registered to " +
                        $"{existing.Descriptor.CollectionType.FullName}; cannot re-register as " +
                        $"{serviceCollection.CollectionType.FullName}.");
                return existing;
            }

            var entry = new PlatformServiceEntry(categoryName, serviceCollection);
            _pending.Add(entry);
            return entry;
        }
    }

    /// <summary>
    /// Enumerates every registered entry, in the order their <c>[ModuleInitializer]</c>s attached
    /// them. No sort is applied -- see <see cref="Configure"/> for how a host expresses ordering.
    /// </summary>
    public static IReadOnlyList<PlatformServiceEntry> Entries()
    {
        EnsureFrozen();
        return _frozenOrder;
    }

    /// <summary>
    /// Calls every registered domain's Configure — skipping any domain already configured manually via its
    /// own dot-walked entry (e.g. <c>PlatformServices.Connection?.Configure(...)</c>), since
    /// <see cref="PlatformServiceEntry.Configure"/> is idempotent. Replaces the manual, per-domain
    /// <c>XxxServiceTypes.Configure(builder, loggerFactory)</c> calls.
    /// </summary>
    /// <remarks>
    /// Domains run in the order their <c>[ModuleInitializer]</c>s attached them. That is not a
    /// dependency order and is not meant to be one -- ordering domains against each other is the
    /// host's call, and it has two ways to express it: run that domain's phase itself before this
    /// collect, or <c>defer</c> it out of the collect and run it manually afterwards.
    /// </remarks>
    /// <param name="builder">The host builder.</param>
    /// <param name="loggerFactory">The host's logger factory, when one is available.</param>
    /// <param name="force">
    /// Run every domain even if its phase is <see cref="PhaseStates.Deferred"/> or has already
    /// <see cref="PhaseStates.Ran"/>.
    /// </param>
    public static IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false)
    {
        EnsureFrozen();
        var _log = loggerFactory?.CreateLogger(typeof(PlatformServices).FullName!) ?? NullLogger.Instance;
        var _failed = new List<string>();
        IGenericResult<IHostApplicationBuilder>? _first = null;

        // Why here, in every environment (FDW-4640): the framework turns these on only when the environment is Development, so Staging and Production never validated and a
        // singleton holding a scoped service (WaitHoldGate) shipped. Hosts do not set the options; this is the one place that does.
        builder.ConfigureContainer(new DefaultServiceProviderFactory(ContainerValidation.Options));
        builder.Services.TryAddSingleton<ContainerValidation>();

        foreach (var entry in _frozenOrder)
        {
            if (!force && entry.ConfigureState.IsDeferred) continue;

            var result = entry.Configure(builder, loggerFactory, force: force);
            if (result.IsFailure)
            {
                // Every domain runs and every failure is named. Returning at the first one reported
                // a single broken domain per boot and hid the rest, so a host with four independent
                // faults needed four deploys to reveal them. The phase still fails -- it just says
                // everything it knows first.
                _failed.Add(entry.CategoryName);
                _first ??= result;
                ServiceTypeLog.PlatformPhaseDomainFailed(
                _log, "Configure", entry.CategoryName,
                _frozenOrder.IndexOf(entry) + 1, _frozenOrder.Length,
                (result.CurrentMessage ?? throw new InvalidOperationException("A failed phase result carries no message; GenericResult refuses that.")).ToString());
                continue;
            }

            builder = result.Value ?? throw new InvalidOperationException($"{entry.CategoryName} Configure succeeded with no builder.");
        }

        if (_failed.Count > 0)
        {
            ServiceTypeLog.PlatformPhaseCompletedWithFailures(
                _log, "Configure", _failed.Count, _frozenOrder.Length, string.Join(", ", _failed));
            return _first!;
        }

        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <summary>
    /// Calls every registered domain's Register — skipping any domain already registered manually via its
    /// own dot-walked entry (e.g. <c>PlatformServices.Connection?.Register(...)</c>), since
    /// <see cref="PlatformServiceEntry.Register"/> is idempotent. Replaces the manual, per-domain
    /// <c>XxxServiceTypes.Register(services, loggerFactory)</c> calls.
    /// </summary>
    /// <remarks>
    /// Domains run in the order their <c>[ModuleInitializer]</c>s attached them. That is not a
    /// dependency order and is not meant to be one -- ordering domains against each other is the
    /// host's call, and it has two ways to express it: run that domain's phase itself before this
    /// collect, or <c>defer</c> it out of the collect and run it manually afterwards.
    /// </remarks>
    /// <param name="builder">The host builder.</param>
    /// <param name="loggerFactory">The host's logger factory, when one is available.</param>
    /// <param name="force">
    /// Run every domain even if its phase is <see cref="PhaseStates.Deferred"/> or has already
    /// <see cref="PhaseStates.Ran"/>.
    /// </param>
    public static IGenericResult<IHostApplicationBuilder> Register(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false)
    {
        EnsureFrozen();
        var _log = loggerFactory?.CreateLogger(typeof(PlatformServices).FullName!) ?? NullLogger.Instance;
        var _failed = new List<string>();
        IGenericResult<IHostApplicationBuilder>? _first = null;

        foreach (var entry in _frozenOrder)
        {
            if (!force && entry.RegisterState.IsDeferred) continue;

            var result = entry.Register(builder, loggerFactory, force: force);
            if (result.IsFailure)
            {
                // Every domain runs and every failure is named. Returning at the first one reported
                // a single broken domain per boot and hid the rest, so a host with four independent
                // faults needed four deploys to reveal them. The phase still fails -- it just says
                // everything it knows first.
                _failed.Add(entry.CategoryName);
                _first ??= result;
                ServiceTypeLog.PlatformPhaseDomainFailed(
                _log, "Register", entry.CategoryName,
                _frozenOrder.IndexOf(entry) + 1, _frozenOrder.Length,
                (result.CurrentMessage ?? throw new InvalidOperationException("A failed phase result carries no message; GenericResult refuses that.")).ToString());
                continue;
            }

        }

        if (_failed.Count > 0)
        {
            ServiceTypeLog.PlatformPhaseCompletedWithFailures(
                _log, "Register", _failed.Count, _frozenOrder.Length, string.Join(", ", _failed));
            return _first!;
        }

        // The registrations are complete here: name every singleton that takes a scoped or a transient service (FDW-4640).
        var lifetimeFindings = ServiceLifetimeCheck.Check(builder.Services, _log, LifetimeCheckSkippedServiceTypes);
        if (lifetimeFindings.Count > 0)
        {
            if (LifetimeFindingsFailStartup)
            {
                ServiceTypeLog.ServiceLifetimeFindings(_log, lifetimeFindings.Count);
                return GenericResult<IHostApplicationBuilder>.Failure(lifetimeFindings);
            }

            ServiceTypeLog.ServiceLifetimeFindingsTolerated(_log, lifetimeFindings.Count);
        }

        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <summary>
    /// Calls every registered domain's Initialize — skipping any domain already initialized manually via its
    /// own dot-walked entry (e.g. <c>PlatformServices.Connection?.Initialize(...)</c>), since
    /// <see cref="PlatformServiceEntry.Initialize"/> is idempotent. Replaces the manual, per-domain
    /// <c>XxxServiceTypes.Initialize(host, loggerFactory)</c> calls.
    /// </summary>
    /// <remarks>
    /// Domains run in the order their <c>[ModuleInitializer]</c>s attached them. That is not a
    /// dependency order and is not meant to be one -- ordering domains against each other is the
    /// host's call, and it has two ways to express it: run that domain's phase itself before this
    /// collect, or <c>defer</c> it out of the collect and run it manually afterwards.
    /// </remarks>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">The host's logger factory, when one is available.</param>
    /// <param name="force">
    /// Run every domain even if its phase is <see cref="PhaseStates.Deferred"/> or has already
    /// <see cref="PhaseStates.Ran"/>.
    /// </param>
    public static IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null, bool force = false)
    {
        EnsureFrozen();
        var _log = loggerFactory?.CreateLogger(typeof(PlatformServices).FullName!) ?? NullLogger.Instance;
        var _failed = new List<string>();
        IGenericResult<IHost>? _first = null;

        // A host whose builder never went through Configure was not built to validate lifetimes (FDW-4640): it does not start.
        if (host.Services.GetService<ContainerValidation>() is null)
        {
            return GenericResult<IHost>.Failure(ServiceTypeLog.ContainerValidationNotConfigured(_log));
        }

        foreach (var entry in _frozenOrder)
        {
            if (!force && entry.InitializeState.IsDeferred) continue;

            var result = entry.Initialize(host, loggerFactory, force: force);
            if (result.IsFailure)
            {
                // Every domain runs and every failure is named. Returning at the first one reported
                // a single broken domain per boot and hid the rest, so a host with four independent
                // faults needed four deploys to reveal them. The phase still fails -- it just says
                // everything it knows first.
                _failed.Add(entry.CategoryName);
                _first ??= result;
                ServiceTypeLog.PlatformPhaseDomainFailed(
                _log, "Initialize", entry.CategoryName,
                _frozenOrder.IndexOf(entry) + 1, _frozenOrder.Length,
                (result.CurrentMessage ?? throw new InvalidOperationException("A failed phase result carries no message; GenericResult refuses that.")).ToString());
                continue;
            }

        }

        if (_failed.Count > 0)
        {
            ServiceTypeLog.PlatformPhaseCompletedWithFailures(
                _log, "Initialize", _failed.Count, _frozenOrder.Length, string.Join(", ", _failed));
            return _first!;
        }

        return GenericResult<IHost>.Success(host);
    }

    /// <summary>
    /// Test-only reset back to the unfrozen, empty state. Never called by production code — the
    /// process-global registry is populated exactly once via module initializers and is not meant to be
    /// re-populated within a process. Internal and only visible to <c>Fdw.Services.Registration.Tests</c>.
    /// </summary>
    internal static void ResetForTesting()
    {
        lock (_gate)
        {
            _pending.Clear();
            LifetimeFindingsFailStartup = true;
            LifetimeCheckSkippedServiceTypes = DefaultLifetimeCheckSkippedServiceTypes;
            _frozenOrder = default;
            _frozen = false;
        }
    }

    private static void EnsureFrozen()
    {
        if (_frozen) return;
        lock (_gate)
        {
            if (_frozen) return;
            _frozenOrder = _pending.ToImmutableArray();
            _frozen = true;
        }
    }
}
