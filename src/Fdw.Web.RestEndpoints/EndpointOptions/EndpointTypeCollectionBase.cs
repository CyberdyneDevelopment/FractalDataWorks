using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.EndpointOptions;

/// <summary>
/// Base for a collection of declared endpoints, one collection per resource.
/// </summary>
/// <typeparam name="TBase">The option base every member of this collection derives from.</typeparam>
/// <remarks>
/// One collection per resource rather than per package: the endpoints over a resource are a CRUD
/// set, and the bases already say so through <c>ResourceName</c>. That granularity is what makes a
/// whole resource switchable in one move — skipping a resource takes its endpoints with it and
/// leaves its siblings alone.
///
/// A resource collection can in turn declare itself a member of its domain's collection, giving
/// three levels a host can switch at: one endpoint, one resource, or the whole domain. A child
/// declares the parent with <c>TypeOption</c> plus <c>TypeOptionName</c>; the parent declares
/// nothing and exposes each child as a <see cref="System.Type"/>.
///
/// Members register through a module initializer, not a static constructor — a call such as
/// <c>SomeEndpoints.ByName(name)</c> binds to an inherited static, and C# does not run the derived
/// type's static constructor for that. It is also what lets a host add its own <c>[TypeOption]</c>
/// to a collection it does not own: replacing a packaged endpoint is a <c>SkipRegistration</c> on
/// the original plus a member declared in the host's own assembly.
/// </remarks>
public abstract class EndpointTypeCollectionBase<TBase> : TypeCollectionBase<TBase, IEndpointTypeOption>, IEndpointTypeCollection
    where TBase : EndpointTypeOptionBase, IEndpointTypeOption
{
    /// <summary>
    /// Gets the endpoints declared against this collection.
    /// </summary>
    /// <remarks>
    /// Abstract because <c>All()</c> is a generated static on the derived collection and this base
    /// cannot name it. Each concrete collection satisfies this with one line returning its own
    /// <c>All()</c> - the bridge between the generated static surface and the polymorphism a
    /// service type needs to cycle collections it does not name.
    /// </remarks>
    public abstract IEnumerable<IEndpointTypeOption> Members { get; }

    // ── The three registration methods, at collection level ─────────────────────────────────────
    // The same shape as the option's, for the same reason one level up: something a whole resource
    // needs — a shared validator, a typed client every one of its endpoints resolves — belongs
    // beside the resource rather than repeated in each member or hoisted into a host.
    //
    // The default bodies are NOT do-nothing. They cycle the members, which is the behaviour a
    // collection exists for; a resource that wants something extra replaces the body and calls
    // the cycle itself.

    /// <summary>Gets the body run during Configure.</summary>
    protected Func<IHostApplicationBuilder, IGenericResult<IHostApplicationBuilder>>? ConfigurationMethod { get; private set; }

    /// <summary>Gets the body run during Register.</summary>
    protected Func<IHostApplicationBuilder, ILoggerFactory?, IGenericResult<IHostApplicationBuilder>>? RegistrationMethod { get; private set; }

    /// <summary>Gets the body run during Initialize.</summary>
    protected Func<IHost, ILoggerFactory?, IGenericResult<IHost>>? InitializationMethod { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether this whole resource should be passed over.
    /// </summary>
    /// <remarks>
    /// The second of three levels. Set this and every member goes with it, without touching the
    /// members themselves — which is what makes "this resource is broken, turn it off, turn it back
    /// on when it is fixed" a single decision rather than one per endpoint.
    /// </remarks>
    public bool SkipRegistration { get; set; }

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

    /// <summary>
    /// Runs Configure for this resource: its own body if one was set, then every member not skipped.
    /// </summary>
    /// <param name="builder">The host builder.</param>
        /// <returns>The builder, or the first failure encountered.</returns>
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

    /// <summary>
    /// Runs Register for this resource: its own body if one was set, then every member not skipped.
    /// </summary>
    /// <param name="builder">The host builder.</param>
        /// <param name="loggerFactory">The logger factory, if the host has one yet.</param>
    /// <returns>The builder, or the first failure encountered.</returns>
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

    /// <summary>
    /// Runs Initialize for this resource: its own body if one was set, then every member not skipped.
    /// </summary>
    /// <param name="host">The built host.</param>
        /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>The host, or the first failure encountered.</returns>
    public IGenericResult<IHost> Initialize(
        IHost host,
        ILoggerFactory? loggerFactory = null)
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

    // Why the members arrive as an argument rather than being read from the collection here: All()
    // is a generated static on the derived collection, and this base cannot name it. The caller
    // that knows the concrete collection passes them in.
    //
    // Why no argument check: nothing to register is a real state, not a mistake. A resource whose
    // members are all skipped, or which has none yet, should register nothing and say so by doing
    // nothing — throwing would turn an ordinary composition choice into a startup failure.
    private static IEnumerable<IEndpointTypeOption> Selected(IEnumerable<IEndpointTypeOption>? members)
        => (members ?? Enumerable.Empty<IEndpointTypeOption>()).Where(m => !m.SkipRegistration);
}
