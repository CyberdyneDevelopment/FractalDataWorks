using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Web.RestEndpoints.EndpointTypeOptions;

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
    // Why: one fixed category for the whole sweep, not the concrete collection's type name. A host
    // filtering its startup log wants "show me what registration did" as a single switch; a
    // per-collection category would make that a wildcard the collections have to keep agreeing on.
    private const string LogCategory = "Fdw.Web.RestEndpoints.EndpointRegistration";

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

    /// <summary>Gets or sets a value indicating whether this whole resource is passed over.</summary>
    /// <remarks>Set this and every member goes with it, without touching the members themselves.</remarks>
    public bool SkipRegistration { get; set; }

    /// <summary>Gets the data store this collection's configuration rows live in.</summary>
    /// <remarks>Virtual so a resource that lives elsewhere says so, rather than the framework
    /// guessing from a name.</remarks>
    /// <summary>Gets this collection's identity as a parent collection sees it.</summary>
    /// <remarks>
    /// A TypeCollection is keyed by an int derived from its type name; a ServiceTypeCollection keys
    /// its members by Guid. This collection is both - a collection of options, and a member of one -
    /// so it carries the Guid the parent needs alongside the int its own members are found by.
    ///
    /// Derived from the name rather than generated: the same collection must be the same
    /// identity in every process that loads it, and Guid.NewGuid() would give a different answer on
    /// each start - so a configuration row written against one run would not be found by the next.
    /// </remarks>
    public new Guid Id => OptionId.Derive(Name);

    /// <summary>Gets the data store this collection's configuration rows live in.</summary>
    /// <remarks>Virtual so a resource that lives elsewhere says so, rather than the framework
    /// guessing from a name.</remarks>
    public virtual string DataStore => "ConfigurationDb";

    /// <summary>Gets the schema within that store.</summary>
    public virtual string PathName => "web";

    /// <summary>Gets the table within that schema.</summary>
    public virtual string Container => Name;

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

    /// <summary>
    /// Runs Configure for this resource: its own body if one was set, then every member not skipped.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="loggerFactory">The host's logger factory, when one is available.</param>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <returns>The builder, or the first failure encountered.</returns>
    public IGenericResult<IHostApplicationBuilder> Configure(IHostApplicationBuilder builder, ILoggerFactory? loggerFactory = null, bool force = false)
    {
        // Why the flag is set here rather than after the work: a phase that failed halfway
        // has already registered whatever came before the failure, and re-entering would do
        // that part twice.
        if (!force && (Configured || SkipConfiguration))
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

    /// <summary>
    /// Runs Register for this resource: its own body if one was set, then every member not skipped.
    /// </summary>
    /// <param name="builder">The host builder.</param>
        /// <param name="loggerFactory">The logger factory, if the host has one yet.</param>
    /// <returns>The builder, or the first failure encountered.</returns>
    /// <remarks>
    /// This is the phase that reports itself. Register is the only one of the three that changes the
    /// container, so it is the only one where "what did this put in" is a question with an answer —
    /// and the answer is measured, as the service-descriptor delta across each call, rather than
    /// taken from what a body says it registers.
    /// </remarks>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    public IGenericResult<IHostApplicationBuilder> Register(
        IHostApplicationBuilder builder,
        ILoggerFactory? loggerFactory = null,
        bool force = false)
    {
        // Why: the factory is null until the host has one, and reporting is not optional work that
        // gets dropped when it is — NullLogger keeps every call below unconditional and silent.
        var logger = loggerFactory?.CreateLogger(LogCategory) ?? NullLogger.Instance;

        if (!force && (Registered || SkipRegistration))
        {
            EndpointRegistrationLog.GroupSkipped(logger, Name);
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        // Why the flag is set here rather than after the work: a phase that failed halfway has already
        // registered whatever came before the failure, and re-entering would do that part twice.
        Registered = true;

        // Why: zero when no body was set is the measurement, not a stand-in for one — a group that
        // declared no registration body of its own contributed nothing to the container.
        var groupServiceCount = 0;
        if (RegistrationMethod is not null)
        {
            var beforeGroup = builder.Services.Count;
            var own = RegistrationMethod(builder, loggerFactory);
            if (own.IsFailure)
            {
                return own;
            }

            groupServiceCount = builder.Services.Count - beforeGroup;
        }

        var endpointCount = 0;

        // Why: every declared member, not Selected(Members) — an endpoint switched off has to be
        // named as switched off. Filtering first is what makes a skipped endpoint indistinguishable
        // from one that was never declared, which is the state this sweep exists to make visible.
        foreach (var member in Members)
        {
            if (member.SkipRegistration)
            {
                EndpointRegistrationLog.EndpointSkipped(logger, Name, member.Name);
                continue;
            }

            var beforeMember = builder.Services.Count;
            var result = member.Register(builder, loggerFactory);
            if (result.IsFailure)
            {
                return result;
            }

            EndpointRegistrationLog.EndpointRegistered(
                logger,
                Name,
                member.Name,
                builder.Services.Count - beforeMember,
                member.EndpointType.Name);
            endpointCount++;
        }

        EndpointRegistrationLog.GroupRegistered(logger, Name, groupServiceCount, endpointCount);
        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <summary>
    /// Runs Initialize for this resource: its own body if one was set, then every member not skipped.
    /// </summary>
    /// <param name="host">The built host.</param>
        /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="force">Run regardless of the skip flag and whether the phase has already run.</param>
    /// <returns>The host, or the first failure encountered.</returns>
    public IGenericResult<IHost> Initialize(
        IHost host,
        ILoggerFactory? loggerFactory = null,
        bool force = false)
    {
        // Why the flag is set here rather than after the work: a phase that failed halfway
        // has already registered whatever came before the failure, and re-entering would do
        // that part twice.
        if (!force && (Initialized || SkipInitialization))
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
