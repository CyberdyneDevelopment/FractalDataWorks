using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Fdw.Services.Abstractions;
using Microsoft.Extensions.Logging;
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
    // Why this is on the base and not in each option's Registration body: DefaultConnectionProvider
    // resolves connections through its OWN factory registry, which it fills once per scope from the
    // funcs options register — but no connection option registered one, so every create failed with
    // "No factory registered for service option type 'MsSql' ... on composed-header path" and no
    // connection could be opened at all. Every connection option needs exactly this registration and
    // already names its factory as TFactory, so the base does it for all of them; an option cannot
    // forget it, and there is nothing to duplicate six times.
    //
    // The func is deferred — it runs in the provider's constructor, once the container exists — so it
    // does not matter that the option's own Register (which puts TFactory into DI) runs after this.
    public override IHostApplicationBuilder Register(
        IHostApplicationBuilder builder,
        ILoggerFactory? loggerFactory,
        string dataStoreName,
        string pathName,
        string containerName)
    {
        DefaultConnectionProvider.Register(
            Name,
            sp => (IServiceFactory<IGenericConnection>)sp.GetRequiredService<TFactory>()!);

        return base.Register(builder, loggerFactory, dataStoreName, pathName, containerName);
    }

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

    // which registers configuration loader using IOptions<List<TConfiguration>> lookup by Name
}
