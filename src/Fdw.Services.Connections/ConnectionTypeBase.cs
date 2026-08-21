using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Fdw.Services.Abstractions;
using Fdw.Services.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Hosting;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// Base class for connection service type definitions that inherit from ServiceTypeBase.
/// Provides metadata, factory creation, and schema discovery support.
/// </summary>
/// <typeparam name="TService">The connection service interface type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating connection service instances.</typeparam>
/// <typeparam name="TConfiguration">The typed body configuration type (e.g. MsSqlConnectionConfiguration).
/// Must implement <see cref="IGenericConfiguration"/> — no longer required to extend <see cref="ConnectionConfiguration"/>
/// after the parent/typed-body split.</typeparam>
public abstract class ConnectionTypeBase<TService, TFactory, TConfiguration> :
    ServiceTypeBase<TService, TFactory, TConfiguration>,
    IConnectionType<TService, TConfiguration, TFactory>,
    ISchemaDiscovery
    where TService : IGenericConnection
    where TConfiguration : class, IGenericConfiguration
    where TFactory : IConnectionFactory<TService, TConfiguration>
{
    /// <inheritdoc />

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionTypeBase{TService,TFactory,TConfiguration}"/> class.
    /// </summary>
    /// <param name="name">The name of this connection service type.</param>
    /// <param name="sectionName">The configuration section name</param>
    /// <param name="displayName">The display name for this service type.</param>
    /// <param name="description">The description of what this service type provides.</param>
    /// <param name="category">The category for this connection type (defaults to "Connection").</param>
    /// <param name="defaultContainerName">The default container name for this connection type.</param>
    protected ConnectionTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null,
        string defaultContainerName = "")
        : base(name, sectionName, displayName, description, category ?? "Connection",
               defaultDataStoreName: "ConfigurationDb",
               defaultPathName: "conn",
               defaultContainerName: defaultContainerName)
    {
        // Why this constructor contributes nothing to a phase: a phase holds one body and the option
        // that declares it owns that body outright. A base contributing here would force every derived
        // option to compose defensively or silently discard what the base left (STC002). The factory
        // registration this used to do is not an option's concern anyway - every connection kind needs
        // it identically - so it belongs to the domain, and ConnectionTypes.Register does it once over
        // the option set it already holds.
    }

    /// <summary>
    /// Discovers the schema containers using the provided connection.
    /// Override in derived types that have a schema discoverer (e.g., MsSqlConnectionType).
    /// Returns an empty list by default — connection types without a discoverer report zero containers.
    /// </summary>
    public virtual Task<IGenericResult<IReadOnlyList<IStorageContainer>>> DiscoverSchema(
        IGenericConnection connection,
        DataStoreDiscoveryOptions options,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<IStorageContainer> empty = new List<IStorageContainer>();
        return Task.FromResult(GenericResult<IReadOnlyList<IStorageContainer>>.Success(empty));
    }

    /// <summary>
    /// Gets the schema discovery types supported by this connection type.
    /// Override in derived types to advertise available discoverers.
    /// </summary>
    /// <remarks>
    /// Why empty default: connection types opt in by overriding. A base that returns nothing
    /// ensures connection types without a discoverer never surface discovery options in the UI.
    /// </remarks>
    public virtual IReadOnlyList<ISchemaDiscoveryType> SupportedDiscoveryTypes =>
        new List<ISchemaDiscoveryType>();

    /// <summary>
    /// Gets the command capabilities this connection supports.
    /// Returns an empty list by default — override in connection types that can act as a
    /// pipeline data source or destination (e.g., <c>MsSqlConnectionType</c>,
    /// <c>HttpConnectionType</c>, <c>FileConnectionType</c>).
    /// </summary>
    /// <remarks>
    /// Why virtual/empty: not every connection type is a data source. Notification sinks,
    /// health-check endpoints, etc. are connections but cannot act as pipeline sources.
    /// Returning an empty list lets the builder hide the capability picker entirely
    /// rather than rendering an empty (and confusing) dropdown.
    /// </remarks>
    public virtual IReadOnlyList<ICommandCapabilityType> SupportedCommands =>
        new List<ICommandCapabilityType>();

    /// <summary>
    /// Gets the default response format for containers reached through this transport when a
    /// container declares no explicit <c>Format</c> (e.g. Http → Json, MsSql → Tabular).
    /// </summary>
    /// <remarks>
    /// Why <see cref="FormatTypes.NotFound"/> base: there is no universally-correct default format,
    /// so the base does NOT invent one. A transport that parses responses MUST override this to
    /// declare its default; a container with no Format under a transport that declares none fails
    /// loud rather than silently defaulting to Tabular (no-fallback rule).
    /// </remarks>
    public virtual IFormatType DefaultResponseFormat => FormatTypes.NotFound;

    /// <summary>
    /// Gets the session contexts this connection kind supports. Defaults to the members of
    /// <see cref="NoSessionContextTypes"/> — the kind has no session-context concept.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Why a virtual property rather than a constructor parameter: <c>NoSessionContextTypes.All()</c>
    /// is not a compile-time constant, so it cannot be an optional parameter's default. An optional
    /// parameter would have to be defaulted in the constructor body as
    /// <c>?? NoSessionContextTypes.All()</c> — a forbidden fallback. A virtual property states the
    /// base position directly and lets a kind that has a scheme override it outright.
    /// </para>
    /// <para>
    /// Why the default is a populated collection rather than an empty list: "this kind has no
    /// session-context concept" is a declared member (<c>NoSessionContextTypes</c>'s <c>None</c>),
    /// not an absence. An empty list would be indistinguishable from a kind that forgot to declare
    /// its contexts. There is no companion <c>bool</c> predicate — support is read off these
    /// members and never off a second signal.
    /// </para>
    /// </remarks>
    public virtual IReadOnlyCollection<ISessionContext> SessionContextTypes =>
        NoSessionContextTypes.All();

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Applies the declared scheme's own selection to <see cref="SessionContextTypes"/> and returns
    /// the governing option's token. Nothing here knows which scheme that is — it reads only the
    /// members the kind declared, so a kind that overrides <see cref="SessionContextTypes"/> gets a
    /// correct partition with no further work, and a kind that does not gets the constant token
    /// <c>NoSessionContextTypes</c>'s sole member returns.
    /// </para>
    /// <para>
    /// Why <c>Single</c> and no "none matched" branch: <see cref="ISessionContext.Governs"/> requires
    /// a scheme's options to partition every authentication context exhaustively and exclusively,
    /// so exactly one governs any input. <c>Single</c> states that invariant and throws if a scheme
    /// ever breaks it, rather than quietly picking a winner or inventing a partition — a wrong
    /// partition here silently shares one caller's rows with another, so failing loud is the only
    /// safe response.
    /// </para>
    /// <para>
    /// A scheme needing different selection semantics overrides this method on its connection type;
    /// the base states the common case rather than closing the set.
    /// </para>
    /// </remarks>
    public virtual string CachePartition(IAuthenticationContext? authenticationContext)
        => SessionContextTypes
            .Single(sessionContext => sessionContext.Governs(authenticationContext))
            .CachePartition(authenticationContext);

    /// <inheritdoc />
    /// <remarks>
    /// Selects the same way <see cref="CachePartition"/> does, and for the same reason: the governing
    /// option owns both answers, so both describe the one session the kind would actually apply.
    /// </remarks>
    public virtual TimeSpan MaxCacheDuration(IAuthenticationContext? authenticationContext)
        => SessionContextTypes
            .Single(sessionContext => sessionContext.Governs(authenticationContext))
            .MaxCacheDuration(authenticationContext);

    // which registers configuration loader using IOptions<List<TConfiguration>> lookup by Name
}
