using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.ComponentTypeOptions;

/// <summary>
/// Base for a collection of declared components, one collection per domain.
/// </summary>
/// <typeparam name="TBase">The option base every member of this collection derives from.</typeparam>
/// <remarks>
/// The counterpart of EndpointTypeCollectionBase, and deliberately the same shape: a skin that
/// wants to replace one component drops it with SkipRegistration and declares its own, exactly as a
/// host replaces an endpoint.
/// </remarks>
public abstract class ComponentTypeCollectionBase<TBase> : TypeCollectionBase<TBase, IComponentTypeOption>, IComponentTypeCollection
    where TBase : ComponentTypeOptionBase, IComponentTypeOption
{
    /// <summary>
    /// Gets the components declared against this collection.
    /// </summary>
    /// <remarks>
    /// Abstract because <c>All()</c> is a generated static on the derived collection and this base
    /// cannot name it. Each concrete collection satisfies this with one line returning its own
    /// <c>All()</c>.
    /// </remarks>
    public abstract IEnumerable<IComponentTypeOption> Members { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Skipped components are excluded: an assembly reaching Blazor's discovery would let a
    /// switched-off component still be found, which is the property this mechanism exists to
    /// prevent.
    /// </remarks>
    public IEnumerable<Assembly> ComponentAssemblies =>
        SkipRegistration
            ? Array.Empty<Assembly>()
            : Selected(Members).Select(m => m.ComponentType.Assembly).Distinct();

    /// <summary>Gets or sets a value indicating whether this whole domain should be passed over.</summary>
    public bool SkipRegistration { get; set; }

    /// <summary>Gets or sets a value indicating whether Configure is switched off.</summary>
    /// <remarks>One flag per phase: they are switched off for different reasons, and a single
    /// flag named for one phase silently governing the other two says something false.</remarks>
    public bool SkipConfiguration { get; set; }

    /// <summary>Gets or sets a value indicating whether Initialize is switched off.</summary>
    public bool SkipInitialization { get; set; }

    /// <summary>Gets a value indicating whether Configure has run.</summary>
    /// <remarks>A phase runs once, so a chained body cannot re-cycle members an earlier one
    /// already drove.</remarks>
    public bool Configured { get; private set; }

    /// <summary>Gets a value indicating whether Register has run.</summary>
    public bool Registered { get; private set; }

    /// <summary>Gets a value indicating whether Initialize has run.</summary>
    public bool Initialized { get; private set; }

    /// <summary>Gets the body run during Configure.</summary>
    protected Func<IHostApplicationBuilder, IGenericResult<IHostApplicationBuilder>>? ConfigurationMethod { get; private set; }

    /// <summary>Gets the body run during Register.</summary>
    protected Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>>? RegistrationMethod { get; private set; }

    /// <summary>Gets the body run during Initialize.</summary>
    protected Func<IHost, ILoggerFactory?, IGenericResult<IHost>>? InitializationMethod { get; private set; }

    /// <summary>Sets the body run during Configure.</summary>
    /// <param name="method">The body.</param>
    public void Configuration(Func<IHostApplicationBuilder, IGenericResult<IHostApplicationBuilder>> method)
        => ConfigurationMethod = method ?? throw new ArgumentNullException(nameof(method));

    /// <summary>Runs <paramref name="method"/> after whatever is already chained.</summary>
    /// <remarks>Prefer this to <see cref="Configuration"/>, which assigns and so discards anything
    /// another contributor already chained. The member cycle is not at risk either way - it lives
    /// in the invoker, and this body runs alongside it rather than instead of it.</remarks>
    /// <param name="method">The body to run after.</param>
    public void AppendConfiguration(Func<IHostApplicationBuilder, IGenericResult<IHostApplicationBuilder>> method)
    {
        if (method is null)
        {
            return;
        }

        var existing = ConfigurationMethod;
        if (existing is null)
        {
            ConfigurationMethod = method;
            return;
        }

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
        if (existing is null)
        {
            ConfigurationMethod = method;
            return;
        }

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
    /// another contributor already chained. The member cycle is not at risk either way - it lives
    /// in the invoker, and this body runs alongside it rather than instead of it.</remarks>
    /// <param name="method">The body to run after.</param>
    public void AppendRegistration(Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> method)
    {
        if (method is null)
        {
            return;
        }

        var existing = RegistrationMethod;
        if (existing is null)
        {
            RegistrationMethod = method;
            return;
        }

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
        if (existing is null)
        {
            RegistrationMethod = method;
            return;
        }

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
    /// another contributor already chained. The member cycle is not at risk either way - it lives
    /// in the invoker, and this body runs alongside it rather than instead of it.</remarks>
    /// <param name="method">The body to run after.</param>
    public void AppendInitialization(Func<IHost, ILoggerFactory?, IGenericResult<IHost>> method)
    {
        if (method is null)
        {
            return;
        }

        var existing = InitializationMethod;
        if (existing is null)
        {
            InitializationMethod = method;
            return;
        }

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
        if (existing is null)
        {
            InitializationMethod = method;
            return;
        }

        InitializationMethod = (host, loggerFactory) =>
        {
            var result = method(host, loggerFactory);
            return result.IsFailure ? result : existing(host, loggerFactory);
        };
    }

    /// <inheritdoc />
    public IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder)
    {
        // Why the flag is set here rather than after the work: a phase that failed halfway
        // has already registered whatever came before the failure, and re-entering would do
        // that part twice.
        if (Configured || SkipConfiguration)
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        Configured = true;

        if (ConfigurationMethod is not null)
        {
            var own = ConfigurationMethod(builder);
            if (own.IsFailure)
            {
                return own;
            }
        }

        foreach (var member in Selected(Members))
        {
            var result = member.Configure(builder);
            if (result.IsFailure)
            {
                return result;
            }
        }

        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <inheritdoc />
    public IGenericResult<IHostApplicationBuilder> Register(
        IHostApplicationBuilder builder,
        ILoggerFactory? loggerFactory = null)
    {
        // Why the flag is set here rather than after the work: a phase that failed halfway
        // has already registered whatever came before the failure, and re-entering would do
        // that part twice.
        if (Registered || SkipRegistration)
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        Registered = true;

        if (RegistrationMethod is not null)
        {
            var own = RegistrationMethod(builder, loggerFactory);
            if (own.IsFailure)
            {
                return own;
            }
        }

        foreach (var member in Selected(Members))
        {
            var result = member.Register(builder, loggerFactory);
            if (result.IsFailure)
            {
                return result;
            }
        }

        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <inheritdoc />
    public IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null)
    {
        // Why the flag is set here rather than after the work: a phase that failed halfway
        // has already registered whatever came before the failure, and re-entering would do
        // that part twice.
        if (Initialized || SkipInitialization)
        {
            return GenericResult<IHost>.Success(host);
        }

        Initialized = true;

        if (InitializationMethod is not null)
        {
            var own = InitializationMethod(host, loggerFactory);
            if (own.IsFailure)
            {
                return own;
            }
        }

        foreach (var member in Selected(Members))
        {
            var result = member.Initialize(host, loggerFactory);
            if (result.IsFailure)
            {
                return result;
            }
        }

        return GenericResult<IHost>.Success(host);
    }

    // Why no argument check: nothing to register is a real state, not a mistake. A domain whose
    // components are all skipped, or which has none yet, registers nothing and says so by doing
    // nothing.
    private static IEnumerable<IComponentTypeOption> Selected(IEnumerable<IComponentTypeOption>? members)
        => (members ?? Enumerable.Empty<IComponentTypeOption>()).Where(m => !m.SkipRegistration);
}
