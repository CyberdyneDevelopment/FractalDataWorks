using System;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.EndpointTypeOptions;

/// <summary>
/// Base for a declared endpoint. Carries the endpoint's type and its registration switch.
/// </summary>
/// <remarks>
/// Identity reaches the collection through this constructor rather than through overridden
/// properties, matching how every other option family in the framework is built —
/// DevelopmentCommandBase, RoslynCommandBase and SqlCommandBase all take their values as
/// constructor arguments and hand a derived id to the base.
/// </remarks>
public abstract class EndpointTypeOptionBase : TypeOptionBase<int, EndpointTypeOptionBase>, IEndpointTypeOption
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointTypeOptionBase"/> class.
    /// </summary>
    /// <param name="name">The option's name — its discriminator within the collection.</param>
    /// <param name="endpointType">The endpoint class this option declares.</param>
    /// <param name="description">What the endpoint does.</param>
    /// <param name="category">The option's category; defaults to <c>Endpoint</c>.</param>
    protected EndpointTypeOptionBase(
        string name,
        Type endpointType,
        string description,
        string? category = null)
        : base(GenerateIdFromName(name), name, name, name, description, category ?? "Endpoint")
    {
        EndpointType = endpointType ?? throw new ArgumentNullException(nameof(endpointType));
    }

    /// <inheritdoc />
    public Type EndpointType { get; }

    /// <inheritdoc />
    public bool SkipRegistration { get; set; }
    /// <summary>Gets or sets a value indicating whether Configure is switched off.</summary>
    /// <remarks>
    /// One flag per phase, because they are switched off for different reasons: a domain may
    /// need its services registered while its post-Build wiring is suppressed, and a single flag
    /// named for one phase silently governing the other two says something false about what it does.
    /// </remarks>
    public bool SkipConfiguration { get; set; }

    /// <summary>Gets or sets a value indicating whether Initialize is switched off.</summary>
    public bool SkipInitialization { get; set; }

    /// <summary>Gets a value indicating whether Configure has run.</summary>
    /// <remarks>
    /// A phase runs once. An endpoint option is reachable from its collection and directly, and
    /// AddTransient plus DeclaredEndpoints.Declare are not free to repeat - declaring the same
    /// endpoint twice is how a route ends up registered twice.
    /// </remarks>
    public bool Configured { get; private set; }

    /// <summary>Gets a value indicating whether Register has run.</summary>
    public bool Registered { get; private set; }

    /// <summary>Gets a value indicating whether Initialize has run.</summary>
    public bool Initialized { get; private set; }

    // ── The three registration methods ──────────────────────────────────────────────────────────
    // Same shape as ServiceTypeBase: a func holding the body, a gerund that sets it, and a method
    // that invokes it. It is here at the ENDPOINT level so an endpoint that needs something of its
    // own - a validator, a typed client, an accessor - registers it beside itself instead of in a
    // host's Program.cs, where the dependency is invisible to anyone reading the endpoint.
    //
    // Defaults are set in the declaration so a func is never null and the invokers never guard.

    /// <summary>Gets the body run during Configure.</summary>
    protected Func<IHostApplicationBuilder, IGenericResult<IHostApplicationBuilder>> ConfigurationMethod { get; private set; }
        = static builder => GenericResult<IHostApplicationBuilder>.Success(builder);

    /// <summary>Gets the body run during Register.</summary>
    protected Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> RegistrationMethod { get; private set; }
        = static (builder, loggerFactory) => GenericResult<IHostApplicationBuilder>.Success(builder);

    /// <summary>Gets the body run during Initialize.</summary>
    protected Func<IHost, ILoggerFactory?, IGenericResult<IHost>> InitializationMethod { get; private set; }
        = static (host, loggerFactory) => GenericResult<IHost>.Success(host);




    /// <summary>Sets the body run during Configure.</summary>
    /// <param name="method">The body.</param>
    public void Configuration(Func<IHostApplicationBuilder, IGenericResult<IHostApplicationBuilder>> method)
    {
        if (method is null)
        {
            return;
        }

        ConfigurationMethod = method;
    }

    /// <summary>Runs <paramref name="method"/> after whatever is already chained.</summary>
    /// <remarks>Prefer this to <see cref="Configuration"/>, which assigns and so discards anything
    /// another contributor already chained.</remarks>
    /// <param name="method">The body to run after.</param>
    public void AppendConfiguration(Func<IHostApplicationBuilder, IGenericResult<IHostApplicationBuilder>> method)
    {
        if (method is null)
        {
            return;
        }

        var existing = ConfigurationMethod;
        ConfigurationMethod = (builder) =>
        {
            var result = existing(builder);
            return result.IsFailure ? result : method(builder);
        };
    }

    /// <summary>Runs <paramref name="method"/> before whatever is already chained.</summary>
    /// <param name="method">The body to run first.</param>
    public void PrependConfiguration(Func<IHostApplicationBuilder, IGenericResult<IHostApplicationBuilder>> method)
    {
        if (method is null)
        {
            return;
        }

        var existing = ConfigurationMethod;
        ConfigurationMethod = (builder) =>
        {
            var result = method(builder);
            return result.IsFailure ? result : existing(builder);
        };
    }

    /// <summary>Sets the body run during Register.</summary>
    /// <param name="method">The body.</param>
    public void Registration(Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> method)
    {
        if (method is null)
        {
            return;
        }

        RegistrationMethod = method;
    }

    /// <summary>Runs <paramref name="method"/> after whatever is already chained.</summary>
    /// <remarks>Prefer this to <see cref="Registration"/>, which assigns and so discards anything
    /// another contributor already chained.</remarks>
    /// <param name="method">The body to run after.</param>
    public void AppendRegistration(Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> method)
    {
        if (method is null)
        {
            return;
        }

        var existing = RegistrationMethod;
        RegistrationMethod = (builder, loggerFactory) =>
        {
            var result = existing(builder, loggerFactory);
            return result.IsFailure ? result : method(builder, loggerFactory);
        };
    }

    /// <summary>Runs <paramref name="method"/> before whatever is already chained.</summary>
    /// <param name="method">The body to run first.</param>
    public void PrependRegistration(Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> method)
    {
        if (method is null)
        {
            return;
        }

        var existing = RegistrationMethod;
        RegistrationMethod = (builder, loggerFactory) =>
        {
            var result = method(builder, loggerFactory);
            return result.IsFailure ? result : existing(builder, loggerFactory);
        };
    }

    /// <summary>Sets the body run during Initialize.</summary>
    /// <param name="method">The body.</param>
    public void Initialization(Func<IHost, ILoggerFactory?, IGenericResult<IHost>> method)
    {
        if (method is null)
        {
            return;
        }

        InitializationMethod = method;
    }

    /// <summary>Runs <paramref name="method"/> after whatever is already chained.</summary>
    /// <remarks>Prefer this to <see cref="Initialization"/>, which assigns and so discards anything
    /// another contributor already chained.</remarks>
    /// <param name="method">The body to run after.</param>
    public void AppendInitialization(Func<IHost, ILoggerFactory?, IGenericResult<IHost>> method)
    {
        if (method is null)
        {
            return;
        }

        var existing = InitializationMethod;
        InitializationMethod = (host, loggerFactory) =>
        {
            var result = existing(host, loggerFactory);
            return result.IsFailure ? result : method(host, loggerFactory);
        };
    }

    /// <summary>Runs <paramref name="method"/> before whatever is already chained.</summary>
    /// <param name="method">The body to run first.</param>
    public void PrependInitialization(Func<IHost, ILoggerFactory?, IGenericResult<IHost>> method)
    {
        if (method is null)
        {
            return;
        }

        var existing = InitializationMethod;
        InitializationMethod = (host, loggerFactory) =>
        {
            var result = method(host, loggerFactory);
            return result.IsFailure ? result : existing(host, loggerFactory);
        };
    }

    /// <summary>Runs this endpoint's Configure body.</summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <returns>The builder, or a failure the caller decides what to do with.</returns>
    public IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, bool force = false)
    {
        if (!force && (Configured || SkipConfiguration))
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        var result = ConfigurationMethod(builder);
        // Why the latch is only set on success: the early return above turns an already-latched phase
        // into an unconditional Success, so latching after a failure records work that never happened
        // as done and reports success for it forever after. Returning first leaves the phase
        // un-latched, so a caller that retries actually retries.
        if (result.IsFailure)
        {
            return result;
        }

        Configured = true;
        return result;
    }

    /// <summary>Runs this endpoint's Register body, and registers the endpoint type itself.</summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="loggerFactory">The logger factory, if the host has one yet.</param>
    /// <returns>The builder, or a failure the caller decides what to do with.</returns>
    /// <remarks>
    /// <see cref="SkipRegistration"/> is honoured by the COLLECTION, not here. An option asked
    /// directly to register does so: skipping is a composition decision the collection makes while
    /// cycling, and burying it here would make a direct call silently do nothing.
    /// </remarks>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    public IGenericResult<IHostApplicationBuilder> Register(
        IHostApplicationBuilder builder,
        ILoggerFactory? loggerFactory = null,
        bool force = false)
    {
        // Why the option checks its own switch rather than trusting the collection to filter it out:
        // an endpoint is reachable directly as well as through its collection, and a switch only half
        // its callers honour is not a switch. AddTransient and Declare are not free to repeat either -
        // declaring the same endpoint twice registers the route twice.
        if (!force && (Registered || SkipRegistration))
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        Registered = true;

        // The endpoint registers ITSELF, in two places, then runs whatever else it needs.
        //
        // DI, so the container can construct it. And DeclaredEndpoints, because FastEndpoints has no
        // per-endpoint registration call: with auto-discovery off, the only way in is the
        // SourceGeneratorDiscoveredTypes list read when AddFastEndpoints runs. Doing both here is
        // what makes SkipRegistration mean something — an endpoint that is skipped is never routed,
        // where a scanner would have found and routed it regardless.
        builder.Services.AddTransient(EndpointType);
        DeclaredEndpoints.Declare(EndpointType);

        return RegistrationMethod(builder, loggerFactory);
    }

    /// <summary>Runs this endpoint's Initialize body.</summary>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <returns>The host, or a failure the caller decides what to do with.</returns>
    public IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null, bool force = false)
    {
        if (!force && (Initialized || SkipInitialization))
        {
            return GenericResult<IHost>.Success(host);
        }

        var result = InitializationMethod(host, loggerFactory);
        // Why the latch is only set on success: the early return above turns an already-latched phase
        // into an unconditional Success, so latching after a failure records work that never happened
        // as done and reports success for it forever after. Returning first leaves the phase
        // un-latched, so a caller that retries actually retries.
        if (result.IsFailure)
        {
            return result;
        }

        Initialized = true;
        return result;
    }

    /// <summary>
    /// Derives an option's name from the endpoint class it declares.
    /// </summary>
    /// <remarks>
    /// The trailing "Endpoint" is trimmed so an option reads as the resource operation —
    /// <c>ListServerSettings</c>, not <c>ListServerSettingsEndpoint</c>.
    ///
    /// Protected rather than private because a collection binds to one non-generic member base, so
    /// every resource declares its own pair: a closed base for the collection and a generic one for
    /// members to close. The generic half needs this to derive its name, and duplicating the trim
    /// per resource is how the convention would drift.
    /// </remarks>
    /// <param name="endpointType">The endpoint class.</param>
    /// <returns>The option name for that endpoint.</returns>
    protected static string DeriveName(Type endpointType)
    {
        if (endpointType is null)
        {
            throw new ArgumentNullException(nameof(endpointType));
        }

        var name = endpointType.Name;
        return name.EndsWith("Endpoint", StringComparison.Ordinal) && name.Length > "Endpoint".Length
            ? name.Substring(0, name.Length - "Endpoint".Length)
            : name;
    }

    /// <summary>
    /// Derives a stable identifier from an option's name.
    /// </summary>
    /// <remarks>
    /// FNV-1a over the name, masked to stay non-negative. The same derivation every other option
    /// base uses, so an endpoint's id is stable across builds and machines — it is a hash of the
    /// name, never a counter or a <c>GetHashCode</c>, which .NET does not guarantee between runs.
    /// </remarks>
    /// <param name="name">The option's name.</param>
    /// <returns>A stable identifier for an option of that name.</returns>
    protected static int GenerateIdFromName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        unchecked
        {
            const int FnvPrime = 0x01000193;
            const int FnvOffsetBasis = (int)0x811C9DC5;
            int hash = FnvOffsetBasis;
            foreach (char c in name)
            {
                hash ^= c;
                hash *= FnvPrime;
            }

            return hash & 0x7FFFFFFF;
        }
    }
}
