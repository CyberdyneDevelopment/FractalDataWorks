using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.ComponentOptions;

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

    /// <summary>Sets the body run during Register.</summary>
    /// <param name="method">The body.</param>
    public void Registration(Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>> method)
        => RegistrationMethod = method ?? throw new ArgumentNullException(nameof(method));

    /// <summary>Sets the body run during Initialize.</summary>
    /// <param name="method">The body.</param>
    public void Initialization(Func<IHost, ILoggerFactory?, IGenericResult<IHost>> method)
        => InitializationMethod = method ?? throw new ArgumentNullException(nameof(method));

    /// <inheritdoc />
    public IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder)
    {
        if (SkipRegistration)
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

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
        if (SkipRegistration)
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

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
        if (SkipRegistration)
        {
            return GenericResult<IHost>.Success(host);
        }

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
