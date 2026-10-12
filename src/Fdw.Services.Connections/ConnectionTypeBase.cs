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
using Fdw.Services.Connections.Abstractions.Authentication;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Connections;

/// <summary>
/// Base class for connection service type definitions that inherit from ServiceTypeBase.
/// Provides metadata, factory creation, and the capabilities a connection kind declares.
/// </summary>
/// <typeparam name="TService">The connection service interface type.</typeparam>
/// <typeparam name="TFactory">The factory type for creating connection service instances.</typeparam>
/// <typeparam name="TConfiguration">The typed body configuration type (e.g. MsSqlConnectionConfiguration).
/// Must implement <see cref="IGenericConfiguration"/> — no longer required to extend <see cref="IConnectionImplementationConfiguration"/>
/// after the parent/typed-body split.</typeparam>
public abstract class ConnectionTypeBase<TService, TFactory, TConfiguration> :
    ServiceTypeBase<TService, TFactory, TConfiguration>,
    IConnectionType<TService, TConfiguration, TFactory>
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
               defaultDataStoreName: () => PlatformConnection.Name,
               defaultPathName: "conn",
               defaultContainerName: defaultContainerName)
    {
        _endpointBehavior = new ConnectionEndpointBehavior(typeof(TConfiguration));
    }

    private readonly ConnectionEndpointBehavior _endpointBehavior;

    /// <summary>
    /// Gets the schema discovery options this connection kind offers. Defaults to
    /// <see cref="NoneSchemaDiscovery"/> alone: the kind cannot discover.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Why a populated default rather than an empty list: "this kind cannot discover" is a declared
    /// member (<see cref="NoneSchemaDiscovery"/>), not an absence. An empty list would be
    /// indistinguishable from a kind that forgot to declare its options. There is no companion
    /// <c>bool</c>; support is read off these members.
    /// </para>
    /// <para>
    /// A kind that can discover overrides this with its strategies and <see cref="NoneSchemaDiscovery"/>
    /// (so one of its connections can decline), and implements <c>ISchemaDiscovery</c> to perform them.
    /// This base no longer implements <c>ISchemaDiscovery</c>: it used to, with a discovery that returned
    /// no containers, so every kind looked discoverable and discovering an unsupported one reported an
    /// empty store instead of failing.
    /// </para>
    /// </remarks>
    public virtual IReadOnlyList<ISchemaDiscoveryType> SupportedDiscoveryTypes =>
        [SchemaDiscoveryTypes.None];

    /// <inheritdoc/>
    /// <remarks>
    /// Narrows to this kind's own configuration and asks <see cref="DecideHostIdentity"/>. A configuration of
    /// another kind cannot be answered for, so it is refused rather than read as "no".
    /// </remarks>
    public IGenericResult<bool> UsesHostIdentity(IConnectionImplementationConfiguration configuration)
    {
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));
        if (configuration is TConfiguration typed)
            return DecideHostIdentity(typed);

        return GenericResult<bool>.Failure(
            Results.ConnectionsResultCodes.ByName("HostIdentityUndetermined"),
            ResultDetails.Create(
                "name", configuration.Name,
                "reason", $"its configuration is not a {typeof(TConfiguration).Name}, which connection type '{Name}' reads"));
    }

    /// <summary>
    /// Decides whether a connection of this kind, configured as <paramref name="configuration"/>, presents the
    /// host's own identity.
    /// </summary>
    /// <param name="configuration">The connection's configuration, already this kind's own.</param>
    /// <returns>The answer, or a <c>HostIdentityUndetermined</c> failure naming why it cannot be given.</returns>
    /// <remarks>
    /// Abstract so every kind states it; there is no answer a kind inherits by saying nothing. A kind with
    /// authentication methods asks the method its configuration names; a kind with none states the fact itself.
    /// </remarks>
    protected abstract IGenericResult<bool> DecideHostIdentity(TConfiguration configuration);

    /// <inheritdoc/>
    /// <remarks>
    /// Narrows to this kind's own configuration and asks <see cref="DescribeCredentials"/>. A configuration of another
    /// kind cannot be answered for, so it is refused rather than read as "no credentials".
    /// </remarks>
    public IGenericResult<ConnectionCredentialSlots> CredentialSlots(IConnectionImplementationConfiguration configuration)
    {
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));
        if (configuration is TConfiguration typed)
            return DescribeCredentials(typed);

        return GenericResult<ConnectionCredentialSlots>.Failure(
            Results.ConnectionsResultCodes.ByName("CredentialsUndetermined"),
            ResultDetails.Create(
                "name", configuration.Name,
                "reason", $"its configuration is not a {typeof(TConfiguration).Name}, which connection type '{Name}' reads"));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Narrows to this kind's own configuration and asks <see cref="BindCredentialReferences"/>; a configuration of
    /// another kind is refused.
    /// </remarks>
    public IGenericResult BindCredentials(
        IConnectionImplementationConfiguration configuration,
        string secretManagerName,
        IReadOnlyDictionary<string, string> secretKeyByField)
    {
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));
        if (configuration is TConfiguration typed)
            return BindCredentialReferences(typed, secretManagerName, secretKeyByField);

        return GenericResult.Failure(
            Results.ConnectionsResultCodes.ByName("CredentialsUndetermined"),
            ResultDetails.Create(
                "name", configuration.Name,
                "reason", $"its configuration is not a {typeof(TConfiguration).Name}, which connection type '{Name}' reads"));
    }

    /// <summary>
    /// Reads which stored credentials a connection of this kind, configured as <paramref name="configuration"/>, uses
    /// and which of them it has.
    /// </summary>
    /// <param name="configuration">The connection's configuration, already this kind's own.</param>
    /// <returns>The slots, or a <c>CredentialsUndetermined</c> failure naming why they cannot be read.</returns>
    /// <remarks>
    /// Abstract so every kind states it. A kind with authentication methods asks the method its configuration names
    /// for its <c>CredentialFields</c>; a kind with none answers <see cref="ConnectionCredentialSlots.None"/>.
    /// </remarks>
    protected abstract IGenericResult<ConnectionCredentialSlots> DescribeCredentials(TConfiguration configuration);

    /// <summary>
    /// Points a connection of this kind at the secrets the server stored for its credential fields.
    /// </summary>
    /// <param name="configuration">The connection's configuration, already this kind's own; changed in place.</param>
    /// <param name="secretManagerName">The secret manager the credentials were stored in.</param>
    /// <param name="secretKeyByField">The secret stored for each credential field.</param>
    /// <returns>Success, or a <c>CredentialsUndetermined</c> failure when this kind cannot take them.</returns>
    protected abstract IGenericResult BindCredentialReferences(
        TConfiguration configuration,
        string secretManagerName,
        IReadOnlyDictionary<string, string> secretKeyByField);

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
    /// Gets the names of the data store types this kind of connection can back. A kind overrides this to declare
    /// them, for example <c>["FileSystem"]</c> for a transport that carries files.
    /// </summary>
    /// <remarks>
    /// The default is the kind's own name, so a kind with a data store type of the same name needs nothing.
    /// </remarks>
    protected virtual IReadOnlyList<string> DataStoreTypeNames => [Name];

    /// <inheritdoc />
    /// <remarks>
    /// Selected from <see cref="DataStoreTypes.All"/>, so a type this deployment did not register is never offered.
    /// </remarks>
    public IReadOnlyList<string> SupportedDataStoreTypes =>
        [.. DataStoreTypeNames.Where(n => DataStoreTypes.All().Any(d => d.Name == n))];

    /// <inheritdoc />
    /// <remarks>
    /// The default reads and writes <typeparamref name="TConfiguration"/> directly, so a kind with no
    /// endpoint code of its own can be created, read and updated. A kind adds what is genuinely its own
    /// from its constructor with <see cref="EndpointAlias"/>, <see cref="EndpointMask"/> and
    /// <see cref="EndpointValidation"/>; a kind that needs different handling altogether overrides this.
    /// </remarks>
    public virtual IConnectionEndpointBehavior<IConnectionImplementationConfiguration> EndpointBehavior => _endpointBehavior;

    /// <summary>
    /// Accepts a deprecated flat request field for one of this kind's configuration properties, so
    /// existing clients keep working while they move to the typed <c>configuration</c> body.
    /// </summary>
    /// <param name="field">The old request field (for example <c>baseUrl</c>).</param>
    /// <param name="property">The <typeparamref name="TConfiguration"/> property it sets (for example <c>Root</c>).</param>
    protected void EndpointAlias(string field, string property) => _endpointBehavior.Alias(field, property);

    /// <summary>
    /// Says which entries of one of this kind's string dictionaries are secret. Without it every entry of
    /// that dictionary is shown masked.
    /// </summary>
    /// <param name="dictionaryProperty">The <typeparamref name="TConfiguration"/> property holding the dictionary.</param>
    /// <param name="mask">Returns the entries with the secret ones replaced by <see cref="ConnectionSecretMask.Value"/>.</param>
    protected void EndpointMask(
        string dictionaryProperty,
        Func<TConfiguration, IReadOnlyDictionary<string, string?>, IDictionary<string, string?>> mask)
        => _endpointBehavior.MaskEntries(dictionaryProperty, (configuration, entries) => mask((TConfiguration)(object)configuration, entries));

    /// <summary>Adds a rule this kind's configuration must pass before it is created or updated.</summary>
    /// <param name="rule">Returns a failure, with its own logged message, to refuse the configuration.</param>
    protected void EndpointValidation(Func<TConfiguration, ILogger, IGenericResult> rule)
        => _endpointBehavior.Validate((configuration, logger) => rule((TConfiguration)(object)configuration, logger));

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
