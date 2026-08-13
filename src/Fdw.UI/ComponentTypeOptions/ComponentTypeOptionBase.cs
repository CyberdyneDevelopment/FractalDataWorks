using System;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.ComponentTypeOptions;

/// <summary>
/// Base for a declared headless component. Carries the provider type and its registration switch.
/// </summary>
/// <remarks>
/// Identity reaches the collection through this constructor rather than through overridden
/// properties, matching every other option family in the framework.
/// </remarks>
public abstract class ComponentTypeOptionBase : TypeOptionBase<int, ComponentTypeOptionBase>, IComponentTypeOption
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ComponentTypeOptionBase"/> class.
    /// </summary>
    /// <param name="name">The option's name.</param>
    /// <param name="componentType">The provider component this option declares.</param>
    /// <param name="description">What the component shows.</param>
    /// <param name="category">The option's category; defaults to <c>Component</c>.</param>
    protected ComponentTypeOptionBase(
        string name,
        Type componentType,
        string description,
        string? category = null)
        : base(GenerateIdFromName(name), name, name, name, description, category ?? "Component")
    {
        ComponentType = componentType ?? throw new ArgumentNullException(nameof(componentType));
    }

    /// <inheritdoc />
    public Type ComponentType { get; }

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
    /// <remarks>A phase runs once, and the option checks its own switch rather than trusting the
    /// collection to filter it out - a component is reachable directly as well as through its
    /// collection, and a switch only half its callers honour is not a switch.</remarks>
    public bool Configured { get; private set; }

    /// <summary>Gets a value indicating whether Register has run.</summary>
    public bool Registered { get; private set; }

    /// <summary>Gets a value indicating whether Initialize has run.</summary>
    public bool Initialized { get; private set; }

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
        => ConfigurationMethod = method ?? throw new ArgumentNullException(nameof(method));

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
        => RegistrationMethod = method ?? throw new ArgumentNullException(nameof(method));

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
        => InitializationMethod = method ?? throw new ArgumentNullException(nameof(method));

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

    /// <inheritdoc />
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

    /// <inheritdoc />
    /// <remarks>
    /// Registers what the component REQUIRES — not the component. That asymmetry with the endpoint
    /// option is deliberate and follows from how Blazor works: FastEndpoints resolves an endpoint
    /// from the container, so an endpoint option must put its type there, but Blazor instantiates a
    /// component from markup and fills its <c>[Inject]</c> properties afterwards. A component in DI
    /// is a registration nothing ever resolves.
    ///
    /// So this body is the seam, and the only thing in it: a validator, an accessor, a cache, a
    /// state container — whatever this component needs and nothing else registers. What makes a
    /// skipped component actually disappear is its assembly never reaching Blazor's discovery,
    /// which the collection handles through <c>ComponentAssemblies</c>.
    ///
    /// <see cref="SkipRegistration"/> is honoured by the COLLECTION while cycling, not here: an
    /// option asked directly to register does so, because skipping is a composition decision.
    /// </remarks>
    /// <param name="builder">The host builder.</param>
    /// <param name="loggerFactory">The host's logger factory, when one is available.</param>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <returns>The builder, or the failure that stopped it.</returns>
    public IGenericResult<IHostApplicationBuilder> Register(
        IHostApplicationBuilder builder,
        ILoggerFactory? loggerFactory = null,
        bool force = false)
    {
        if (!force && (Registered || SkipRegistration))
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        var result = RegistrationMethod(builder, loggerFactory);
        // Why the latch is only set on success: the early return above turns an already-latched phase
        // into an unconditional Success, so latching after a failure records work that never happened
        // as done and reports success for it forever after. Returning first leaves the phase
        // un-latched, so a caller that retries actually retries.
        if (result.IsFailure)
        {
            return result;
        }

        Registered = true;
        return result;
    }

    /// <inheritdoc />
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
    /// Derives an option's name from the component it declares.
    /// </summary>
    /// <remarks>
    /// The trailing "Provider" is trimmed so an option reads as the thing rather than the mechanism
    /// — <c>Settings</c>, not <c>SettingsProvider</c>. Protected because each domain declares a
    /// non-generic base for its collection plus a generic one for members, and the generic half
    /// needs this; duplicating the trim per domain is how the convention would drift.
    /// </remarks>
    /// <param name="componentType">The provider component.</param>
    /// <returns>The option name for that component.</returns>
    protected static string DeriveName(Type componentType)
    {
        if (componentType is null)
        {
            throw new ArgumentNullException(nameof(componentType));
        }

        var name = componentType.Name;
        return name.EndsWith("Provider", StringComparison.Ordinal) && name.Length > "Provider".Length
            ? name.Substring(0, name.Length - "Provider".Length)
            : name;
    }

    /// <summary>
    /// Derives a stable identifier from an option's name.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <returns>A stable non-negative id.</returns>
    protected static int GenerateIdFromName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException(nameof(name));
        }

        unchecked
        {
            const int offset = (int)2166136261;
            const int prime = 16777619;
            var hash = offset;
            foreach (var c in name)
            {
                hash = (hash ^ c) * prime;
            }

            return hash & 0x7FFFFFFF;
        }
    }
}
