using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Logging;
using Fdw.Conventions;
using Fdw.Services.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Authentication.Flow;

/// <summary>
/// Assembles flows from their rows and checks each one before anybody tries to log in.
/// </summary>
/// <remarks>
/// Caches after the first load. Flows change by <c>UPDATE</c> rather than deployment, so a host
/// picks up a change on restart — deliberate, because a flow changing under a login in progress is a
/// harder thing to reason about than a restart.
/// The one exception is a change made through <see cref="IAuthenticationFlowAdministration"/>, which
/// calls <see cref="LoadAndValidate"/> after writing: each flow's entry is replaced whole, so a sign-in
/// already holding the old flow finishes on it and the next one runs the new (FDW-912).
/// <para>
/// One flow's failure never blocks another's: every row is validated independently and gets its own
/// cache entry either way. A flow that is seeded but not yet fully wired — a step whose package
/// isn't referenced yet, say — fails loud for callers asking for THAT flow by name, and stays
/// entirely invisible to every other flow's resolution.
/// </para>
/// </remarks>
public sealed class AuthenticationFlowProvider : IAuthenticationFlowProvider
{
    /// <summary>
    /// One row's outcome. <see cref="Flow"/> set means valid and ready to run; unset means
    /// <see cref="InvalidReason"/> says why — never both, never neither. Kept private to this
    /// provider: <see cref="AuthenticationFlow"/> itself carries no validity concept, because every
    /// other holder of one (the runner included) already assumes it is good.
    /// </summary>
    private sealed record CachedFlow(AuthenticationFlow? Flow, string? InvalidReason, bool Disabled = false);

    private readonly IAuthenticationFlowConfigurationProvider _flows;
    private readonly ConcurrentDictionary<string, CachedFlow> _cache = new(StringComparer.Ordinal);
    private readonly IAuthenticationContextAccessor _authContextAccessor;
    private readonly Func<string, IAuthenticationStep?> _step;
    private readonly ILogger<AuthenticationFlowProvider> _logger;

    /// <summary>Initializes a new instance of the <see cref="AuthenticationFlowProvider"/> class.</summary>
    /// <param name="flows">Reads the flow rows.</param>
    /// <param name="authContextAccessor">Establishes context for platform flow configuration reads.</param>
    /// <param name="step">
    /// How a name becomes a step -- the same lookup the runner is handed. The registration passes the
    /// collection, which is the registry; a test passes steps that vary per case, which is what proving
    /// the load-time checks (settings, order, requirements) needs.
    /// </param>
    /// <param name="logger">The logger.</param>
    public AuthenticationFlowProvider(
        IAuthenticationFlowConfigurationProvider flows,
        IAuthenticationContextAccessor authContextAccessor,
        Func<string, IAuthenticationStep?> step,
        ILogger<AuthenticationFlowProvider>? logger = null)
    {
        _flows = flows ?? throw new ArgumentNullException(nameof(flows));
        _authContextAccessor = authContextAccessor ?? throw new ArgumentNullException(nameof(authContextAccessor));
        _step = step ?? throw new ArgumentNullException(nameof(step));
        _logger = logger ?? NullLogger<AuthenticationFlowProvider>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<AuthenticationFlow>> Get(
        string flowName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(flowName))
            return GenericResult<AuthenticationFlow>.Failure(FlowProviderLog.NameMissing(_logger));

        if (_cache.TryGetValue(flowName, out var cached))
            return Resolve(flowName, cached);

        var loaded = await LoadAndValidate(cancellationToken).ConfigureAwait(false);
        if (loaded.IsFailure)
            return loaded.ToNewResult<AuthenticationFlow>();

        // Coded, so a caller can answer "you named nothing" (a 4xx) apart from "the host is broken"
        // (the store unreadable, or the flow configured and invalid) without reading message text.
        return _cache.TryGetValue(flowName, out var flow)
            ? Resolve(flowName, flow)
            : GenericResult<AuthenticationFlow>.Chain(
                AuthenticationResultCodes.ByName("FlowNotFound"),
                GenericResult.Failure(FlowProviderLog.NoSuchFlow(_logger, flowName, string.Join(", ", ValidNames()))));
    }

    // I5 — a flow that failed validation is a known, named failure, not an unknown selection: the
    // two produce different messages so a caller (and whoever reads the log) can tell "you typed the
    // wrong name" apart from "this flow is configured and broken, see why it failed above".
    private IGenericResult<AuthenticationFlow> Resolve(string flowName, CachedFlow cached)
    {
        if (cached.Flow is { } flow)
        {
            FlowProviderLog.FlowServed(_logger, flowName);
            return GenericResult<AuthenticationFlow>.Success(flow);
        }

        // Switched off by an operator: the row is kept, the flow is refused. Distinct from "no such
        // flow" so the log says which, and distinct from "invalid" because nothing is broken.
        if (cached.Disabled)
            return GenericResult<AuthenticationFlow>.Chain(
                AuthenticationResultCodes.ByName("FlowDisabled"),
                GenericResult.Failure(FlowProviderLog.FlowDisabled(_logger, flowName)));

        return GenericResult<AuthenticationFlow>.Failure(
            FlowProviderLog.FlowKnownInvalid(_logger, flowName, cached.InvalidReason ?? "unknown reason"));
    }

    /// <inheritdoc />
    public async Task<IGenericResult<IReadOnlyList<AuthenticationFlow>>> Get(
        CancellationToken cancellationToken = default)
    {
        // Load on an empty cache rather than unconditionally: a caller asking what is on offer
        // before anything has asked for a flow by name would otherwise get an empty list back and
        // read it as "this host federates with nobody", which is the same false answer a Login
        // screen has already shipped once.
        if (_cache.IsEmpty)
        {
            var loaded = await LoadAndValidate(cancellationToken).ConfigureAwait(false);
            if (loaded.IsFailure)
                return loaded.ToNewResult<IReadOnlyList<AuthenticationFlow>>();
        }

        return GenericResult<IReadOnlyList<AuthenticationFlow>>.Success(
            [.. _cache.Values
                .Where(v => v.Flow is not null)
                .Select(v => v.Flow!)
                .OrderBy(f => f.Name, StringComparer.Ordinal)]);
    }

    // Only the flows that are actually usable — naming a broken one back to the caller as a
    // "configured" alternative would just move the same failure to whatever they tried next.
    private IEnumerable<string> ValidNames()
        => _cache.Where(kv => kv.Value.Flow is not null)
                 .Select(kv => kv.Key)
                 .OrderBy(k => k, StringComparer.Ordinal);

    /// <inheritdoc />
    public async Task<IGenericResult> LoadAndValidate(CancellationToken cancellationToken = default)
    {
        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);
        var rows = await _flows.Get(cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure)
            return GenericResult.Failure(FlowProviderLog.RowsUnreadable(_logger, "auth.AuthenticationFlow"));

        // Every row is judged on its own and gets its own cache entry either way.
        foreach (var row in rows.Value ?? [])
            _cache[row.Name] = Load(row);

        FlowProviderLog.FlowsLoaded(_logger, ValidNames().Count(), _cache.Count);
        return GenericResult.Success();
    }

    // One row's outcome: the assembled flow, the reason it is known-invalid, or switched off.
    [ConventionOverride(MaxCyclomaticComplexity = 19)]  // Each check an independent rule (switched off, steps, execution lifetime, execution key, ReturnUrls/CodeLifetime, code lifetime, step validation) that ends the row with its reason
    private CachedFlow Load(IAuthenticationFlowImplementationConfiguration row)
    {
        // Checked before anything else: a switched-off flow is not validated, because an operator
        // may switch a flow off precisely while its configuration is being changed. Validation
        // runs when it is switched back on and the host restarts.
        if (!row.IsEnabled)
        {
            FlowProviderLog.FlowLoadedDisabled(_logger, row.Name);
            return new CachedFlow(null, null, Disabled: true);
        }

        // A flow's steps hang from its own row, so they arrive with it.
        var ordered = row.Steps
            .OrderBy(s => s.StepOrder)
            .ToList();

        if (ordered.Count == 0)
        {
            FlowProviderLog.FlowHasNoSteps(_logger, row.Name);
            return new CachedFlow(null, "has no steps");
        }

        // The column is text (an ISO 8601 duration); the flow model holds the parsed span, so a
        // row that cannot be read as one is a known-invalid flow rather than a thrown format
        // exception at startup.
        if (!TryLifetime(row.ExecutionLifetime, "execution lifetime", out var lifetime, out var lifetimeReason))
        {
            return new CachedFlow(null, lifetimeReason);
        }

        // Why the key is conditional (FDW-897): it encrypts only what a flow stores -- a suspended
        // execution and the token behind a one-time browser code. A flow stores state when a step
        // federates (the caller leaves and comes back) or when it returns a browser (ReturnUrls).
        // One that does neither (password login, tenant switch) never touches a key and names
        // none. One that does must name both: which secret encrypts it is the deployment's
        // per-flow decision, so a missing key makes the flow known-invalid rather than defaulted.
        var namesKey = !string.IsNullOrWhiteSpace(row.SecretManagerName);
        if (namesKey == string.IsNullOrWhiteSpace(row.SecretKeyName))
        {
            return new CachedFlow(null, "execution key location is half-set: SecretManagerName and SecretKeyName are named together or not at all");
        }

        // A session-delivered flow stores state too (FDW-903): the claims its session stands for
        // are encrypted under this flow's key, so it names one like any other flow that stores.
        var deliversSession = !string.IsNullOrWhiteSpace(row.SessionServiceName);
        if (!namesKey
            && (row.ReturnUrls is not null
                || deliversSession
                || ordered.Any(s => _step(s.StepName) is IFederatingAuthenticationStep)))
        {
            return new CachedFlow(null, "stores state (a federating step, ReturnUrls or a session) but names no execution key (SecretManagerName/SecretKeyName)");
        }

        // Why CodeLifetime travels with ReturnUrls (FDW-898): only a flow that returns a browser
        // issues the one-time code, and how long that code stays redeemable is the deployment's
        // per-flow decision -- never a compiled-in constant. One set without the other is a
        // configuration nobody meant, so the flow is known-invalid rather than half-working.
        // A session-delivered flow (FDW-903) issues one too: the callback lands on the host the
        // identity provider was told to return to, which need not be the origin the browser app
        // calls through, so the session id crosses to that origin behind the code and /auth/redeem
        // sets the cookie there.
        if ((row.ReturnUrls is null) != (row.CodeLifetime is null))
        {
            return new CachedFlow(null, "ReturnUrls and CodeLifetime are set together or not at all: only a flow that returns a browser issues a one-time code");
        }

        TimeSpan? codeLifetime = null;
        if (row.CodeLifetime is not null)
        {
            if (!TryLifetime(row.CodeLifetime, "code lifetime", out var parsedCodeLifetime, out var codeLifetimeReason))
            {
                return new CachedFlow(null, codeLifetimeReason);
            }

            codeLifetime = parsedCodeLifetime;
        }

        var steps = ordered
            .Select(s => new AuthenticationFlowStep { Name = s.StepName, Configuration = s.Configuration })
            .ToList();

        var valid = Validate(row.Name, steps);
        if (valid.IsFailure)
        {
            // Why: Validate() already logged the specific reason (StepNotAvailable/OrderInvalid/
            // StepConfigurationRejected) as a side effect of building this failure. Stashing
            // CurrentMessage here is so Get() can report it again to whoever asked for THIS flow by
            // name, without this row's problem stopping any other row from loading.
            return new CachedFlow(null, valid.CurrentMessage ?? "failed validation");
        }

        var flow = new AuthenticationFlow
        {
            Name = row.Name,
            Steps = steps,
            RequiredPermissions = valid.Value!,
            Audience = row.Audience,
            MinimumAcr = row.MinimumAcr,
            ExecutionLifetime = lifetime,
            SecretManagerName = row.SecretManagerName,
            SecretKeyName = row.SecretKeyName,
            // Whitespace-separated in the column; a NULL column means the flow returns no browser.
            ReturnUrls = row.ReturnUrls is null
                ? null
                : [.. row.ReturnUrls.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)],
            CodeLifetime = codeLifetime,
            SessionServiceName = deliversSession ? row.SessionServiceName : null,
        };

        FlowProviderLog.FlowAssembled(_logger, row.Name, ordered.Count);
        return new CachedFlow(flow, null);
    }

    /// <summary>
    /// Checks that every step a flow names exists, that the order satisfies each one, and that each
    /// configured step accepts the settings this flow gives it.
    /// </summary>
    /// <param name="flowName">The flow.</param>
    /// <param name="steps">Its steps, in order.</param>
    /// <returns>The permissions the flow's gated steps require; empty for a flow open to every active user.</returns>
    /// <remarks>
    /// Run when a flow's row loads, so a flow naming a step whose package is absent, ordering two steps
    /// wrongly, or handing a step settings it cannot read fails with the problem named the first time
    /// that flow is asked for, rather than at 3am on someone's login. The collection answers by name and
    /// what it returns IS the step, so its declarations are read straight off it. A failure here is this
    /// flow's alone: the caller stashes the reason and moves on to the next row.
    /// </remarks>
    private IGenericResult<IReadOnlyList<string>> Validate(string flowName, IReadOnlyList<AuthenticationFlowStep> steps)
    {
        var established = new HashSet<IContextElement>();
        var required = new List<string>();

        foreach (var entry in steps)
        {
            if (_step(entry.Name) is not { } step)
                return GenericResult<IReadOnlyList<string>>.Failure(FlowProviderLog.StepNotAvailable(
                    _logger, flowName, entry.Name,
                    string.Join(", ", AuthenticationStepTypes.All().Values.Select(o => o.Name))));

            var missing = step.Requires.Where(r => !established.Contains(r)).ToList();
            if (missing.Count > 0)
                return GenericResult<IReadOnlyList<string>>.Failure(FlowProviderLog.OrderInvalid(
                    _logger, flowName, entry.Name, string.Join(", ", missing)));

            if (step is IConfiguredAuthenticationStep configured)
            {
                var accepted = configured.ValidateConfiguration(entry.Configuration);
                if (accepted.IsFailure)
                    return GenericResult<IReadOnlyList<string>>.Failure(FlowProviderLog.StepConfigurationRejected(
                        _logger, flowName, entry.Name, accepted.CurrentMessage ?? "the step gave no reason"));
            }

            if (step is IPermissionGatedAuthenticationStep gated)
            {
                var permission = gated.RequiredPermission(entry.Configuration);
                if (permission.IsFailure || permission.Value is not { Length: > 0 } name)
                    return GenericResult<IReadOnlyList<string>>.Failure(FlowProviderLog.StepConfigurationRejected(
                        _logger, flowName, entry.Name, permission.CurrentMessage ?? "the step names no permission"));

                required.Add(name);
            }

            foreach (var contributed in step.Contributes)
                established.Add(contributed);
        }

        return GenericResult<IReadOnlyList<string>>.Success(required);
    }

    private static bool TryLifetime(string? configured, string lifetimeName, out TimeSpan lifetime, out string reason)
    {
        lifetime = default;
        if (configured is not { Length: > 0 } value)
        {
            reason = $"{lifetimeName} is unset";
            return false;
        }

        try
        {
            lifetime = System.Xml.XmlConvert.ToTimeSpan(value);
            reason = string.Empty;
            return true;
        }
        catch (FormatException ex)
        {
            reason = $"{lifetimeName} '{value}' is not a duration: {ex.Message}";
            return false;
        }
    }
}
