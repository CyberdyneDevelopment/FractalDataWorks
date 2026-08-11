using System;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.ComponentOptions;

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

    /// <summary>Sets the body run during Register.</summary>
    /// <param name="method">The body.</param>
    public void Registration(Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> method)
        => RegistrationMethod = method ?? throw new ArgumentNullException(nameof(method));

    /// <summary>Sets the body run during Initialize.</summary>
    /// <param name="method">The body.</param>
    public void Initialization(Func<IHost, ILoggerFactory?, IGenericResult<IHost>> method)
        => InitializationMethod = method ?? throw new ArgumentNullException(nameof(method));

    /// <inheritdoc />
    public virtual IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder)
        => ConfigurationMethod(builder);

    /// <inheritdoc />
    /// <remarks>
    /// The component registers ITSELF in DI before running whatever else it needs, so a skipped
    /// component is never resolvable — where a scan would have found and registered it regardless.
    /// <see cref="SkipRegistration"/> is honoured by the COLLECTION while cycling, not here: an
    /// option asked directly to register does so, because skipping is a composition decision.
    /// </remarks>
    public virtual IGenericResult<IHostApplicationBuilder> Register(
        IHostApplicationBuilder builder,
        ILoggerFactory? loggerFactory = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddTransient(ComponentType);
        return RegistrationMethod(builder, loggerFactory);
    }

    /// <inheritdoc />
    public virtual IGenericResult<IHost> Initialize(IHost host, ILoggerFactory? loggerFactory = null)
        => InitializationMethod(host, loggerFactory);

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
