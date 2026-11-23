using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Schema.Ddl;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions.Authentication;
using Fdw.Services.SecretManagers.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Interface for connection service types.
/// </summary>
/// <typeparam name="TService">The connection service interface type.</typeparam>
/// <typeparam name="TConfiguration">The configuration type for the connection service.</typeparam>
/// <typeparam name="TFactory">The factory type for creating connection service instances.</typeparam>
public interface IConnectionType<TService, TConfiguration, TFactory> : IServiceType<Guid, TService, TFactory, TConfiguration>, IConnectionType
    where TService : IGenericConnection
    where TConfiguration : IGenericConfiguration
    where TFactory : IConnectionFactory<TService, TConfiguration>
{
}

/// <summary>
/// Non-generic interface for connection service types.
/// </summary>
public interface IConnectionType : IServiceType
{

    /// <summary>
    /// Gets the capabilities this connection kind has beyond the data commands: modes (unit of work, soft
    /// delete, compound query, streaming, vectorized reads) and what the kind is for (file read, HTTP request).
    /// Every data connection answers every data command, natively where its translator collection has one and
    /// by composing simpler commands where it has none, so no data command is listed here.
    /// The pipeline builder reads this list to populate the capability picker and then renders
    /// the selected capability's <see cref="ICommandCapabilityType.ConfigurationFields"/>
    /// (or its own editor when that list is empty).
    /// Returns an empty list for connection types that cannot act as a data source or destination
    /// (e.g., notification sinks, health-check endpoints).
    /// </summary>
    IReadOnlyList<ICommandCapabilityType> SupportedCommands { get; }

    /// <summary>
    /// Gets the schema discovery options this connection kind offers: the strategies it can discover
    /// a store's schema with, plus <see cref="NoneSchemaDiscovery"/> when a connection of this kind may
    /// decline. Returns only <see cref="NoneSchemaDiscovery"/> for kinds that cannot discover.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Whether a kind can discover is read off these members (a member whose
    /// <see cref="ISchemaDiscoveryType.DiscoversSchema"/> is true), never off a separate flag beside
    /// them. A connection names the one it uses in
    /// <see cref="IConnectionImplementationConfiguration.SchemaDiscovery"/>, which must be one of these.
    /// </para>
    /// <para>
    /// A declaration, not the discovery itself: a kind that declares a strategy performs it through
    /// <c>ISchemaDiscovery</c>, where the services the strategy needs are resolved.
    /// </para>
    /// </remarks>
    IReadOnlyList<ISchemaDiscoveryType> SupportedDiscoveryTypes { get; }

    /// <summary>
    /// Gets the generator of the DDL text of this kind's engine (FDW-5959): the dialect of a connection of this type, so that
    /// whoever writes DDL for a connection asks its type and names no engine itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Why a member of the type and not a lookup by the type's name: the connection type is where a connection's
    /// capabilities are declared (<see cref="SupportedCommands"/>, <see cref="SupportedDiscoveryTypes"/>), so its DDL dialect is
    /// declared beside them, and the engine's text stays in the connection implementation.
    /// </para>
    /// <para>
    /// A kind that has no generator answers the coded refusal <c>DdlDialectNotOffered</c>, naming its type: there is no default
    /// dialect, and never another engine's text. <c>ConnectionTypeBase</c> answers it unless a kind overrides it.
    /// </para>
    /// </remarks>
    /// <returns>The generator, or the <c>DdlDialectNotOffered</c> failure.</returns>
    IGenericResult<IDdlGenerator> DdlGenerator();

    /// <summary>
    /// Builds the provider of this kind that turns a connection's configuration into a connection, from the kind's factory and the secret
    /// managers it may read secrets from (FDW-6360).
    /// </summary>
    /// <param name="factory">The kind's factory, as it was resolved from the container.</param>
    /// <param name="secretManagers">The provider the kind's provider reads a connection's secret from. At start-up this is the one built from the schema's declared managers, never the container's.</param>
    /// <param name="loggerFactory">Where the provider's refusals are logged; nothing is logged when it is absent.</param>
    /// <returns>The provider, or the coded failure: <c>ConnectionKindNotRegistered</c> for the empty kind the collection answers for an unknown name, <c>ConnectionFactoryOfAnotherKind</c> for a factory that is not this kind's.</returns>
    /// <remarks>
    /// One path for every host: the gateways a host opens before any configuration store exists and the connections it reads from that store are built by the same
    /// provider of the same kind. No kind has a default; each states how its provider is built.
    /// </remarks>
    IGenericResult<IImplementationServiceProvider<IGenericConnection, IConnectionImplementationConfiguration>> ImplementationProvider(
        IConnectionFactory factory, ISecretManagerProvider secretManagers, ILoggerFactory? loggerFactory);

    /// <summary>
    /// Gets the names of the data store types a data store over this kind of connection may have, limited to
    /// the data store types this deployment registered. The first is the default when a create request names
    /// none. Empty when none of the kind's data store types is registered.
    /// </summary>
    /// <remarks>
    /// A data store describes what a connection holds (tables, endpoints, files with their paths, containers
    /// and fields), and that does not depend on how the bytes are carried, so a kind names the data store types
    /// it can back instead of every transport needing one of its own. Names rather than the types themselves,
    /// because <c>IDataStoreType</c> sits in a package above this one.
    /// </remarks>
    IReadOnlyList<string> SupportedDataStoreTypes { get; }

    /// <summary>
    /// Gets the default response format for containers reached through this transport when a
    /// container declares no explicit <c>Format</c> (e.g. Http → Json). Returns the
    /// <c>FormatTypes.NotFound</c> sentinel for transports that declare none — callers fail loud
    /// rather than inventing a default (no-fallback rule).
    /// </summary>
    IFormatType DefaultResponseFormat { get; }

    /// <summary>
    /// Gets how the connection endpoints read, update and show this kind's configuration. Every kind
    /// has one: the default binds straight into the kind's configuration class, and a kind adds only
    /// what is genuinely its own (a legacy field alias, which dictionary entries are safe to show, a
    /// validation rule).
    /// </summary>
    IConnectionEndpointBehavior<IConnectionImplementationConfiguration> EndpointBehavior { get; }

    /// <summary>
    /// Decides whether a connection of this kind, configured as <paramref name="configuration"/>, presents the
    /// host's own identity rather than a credential the connection carries.
    /// </summary>
    /// <param name="configuration">The connection's configuration.</param>
    /// <returns>
    /// The answer, or a <c>HostIdentityUndetermined</c> failure when the configuration does not say enough to
    /// decide -- an authentication method this kind does not know, or a configuration of another kind.
    /// </returns>
    /// <remarks>
    /// Each kind answers from what it is: a kind with authentication methods asks the method its configuration
    /// names (<c>ConnectionAuthenticationTypeBase.UsesHostIdentity</c>); a kind with none states the fact for
    /// every connection it makes. A connection that acts as the host reaches whatever the host's identity reaches,
    /// so the connection endpoints let only a platform administrator create or change one (FDW-1068).
    /// </remarks>
    IGenericResult<bool> UsesHostIdentity(IConnectionImplementationConfiguration configuration);

    /// <summary>
    /// Reads which stored credentials a connection of this kind, configured as <paramref name="configuration"/>,
    /// uses and which of them it has.
    /// </summary>
    /// <param name="configuration">The connection's configuration.</param>
    /// <returns>
    /// The credential slots, or a <c>CredentialsUndetermined</c> failure when the configuration does not say which
    /// authentication method it uses, or is of another kind.
    /// </returns>
    /// <remarks>
    /// Names only, never values: a credential is written through the connection and never read back (FDW-1086).
    /// </remarks>
    IGenericResult<ConnectionCredentialSlots> CredentialSlots(IConnectionImplementationConfiguration configuration);

    /// <summary>
    /// Says whether a connection of this kind, configured as <paramref name="configuration"/>, reads a stored secret (FDW-6387, FDW-6331).
    /// </summary>
    /// <param name="configuration">The connection's configuration.</param>
    /// <returns>
    /// <see langword="true"/> when the authentication method the connection names has a secret credential; <see langword="false"/> when
    /// the connection names no authentication method, or one that reads none; a <c>CredentialsUndetermined</c> failure when it names a
    /// method this kind does not know, or the configuration is of another kind.
    /// </returns>
    /// <remarks>
    /// A connection that names no method reads no secret, so a start-up check that proves a secret is readable does not ask it for its
    /// credential slots. This is the question that check asks; <see cref="CredentialSlots"/> still refuses a connection that names no
    /// method, for the callers that save and read stored connections. No kind answers a missing method as a method named <c>None</c>.
    /// </remarks>
    IGenericResult<bool> ReadsStoredSecret(IConnectionImplementationConfiguration configuration);

    /// <summary>
    /// Points a connection's credential fields at the secrets the server stored for it.
    /// </summary>
    /// <param name="configuration">The connection's configuration, changed in place.</param>
    /// <param name="secretManagerName">The secret manager the credentials were stored in.</param>
    /// <param name="secretKeyByField">The secret stored for each credential field the method uses.</param>
    /// <returns>Success, or a <c>CredentialsUndetermined</c> failure when the configuration cannot take them.</returns>
    /// <remarks>
    /// Only the server calls this, after writing the values: a caller never chooses the secret manager or the
    /// secret a connection reads (FDW-1086).
    /// </remarks>
    IGenericResult BindCredentials(
        IConnectionImplementationConfiguration configuration,
        string secretManagerName,
        IReadOnlyDictionary<string, string> secretKeyByField);

    /// <summary>
    /// Gets the session contexts this connection kind supports — the ways it can describe the
    /// calling principal to the store it opens. Returns the members of
    /// <see cref="NoSessionContextTypes"/> for kinds that have no session-context concept.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A declared capability surface, at the same status as <see cref="SupportedCommands"/> and
    /// <see cref="SupportedDiscoveryTypes"/>: it states what the kind can do, for validation, logging and
    /// the option picker. It is not itself the runtime selection — which context governs a given
    /// call is decided by the scheme that owns these options.
    /// </para>
    /// <para>
    /// <b>Never read this assuming a particular scheme's members exist.</b> No <c>ByName("System")</c>,
    /// no <c>First(x =&gt; x.Name == "Deny")</c>, no switch on member names outside the package that
    /// owns the scheme. The members are whatever collection the connection type points at, and a
    /// consumer running a different row-level-security design points it at a different collection
    /// whose members mean entirely different things.
    /// </para>
    /// </remarks>
    IReadOnlyCollection<ISessionContext> SessionContextTypes { get; }

    /// <summary>
    /// Gets the cache partition a caller with <paramref name="authenticationContext"/> reads under
    /// through this connection kind — an opaque token identifying that caller's visibility scope.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why the kind exposes this rather than consumers selecting themselves.</b> Choosing which
    /// of <see cref="SessionContextTypes"/> governs a caller is the scheme's own rule, and the
    /// remarks on that property forbid outside code from selecting on member names. This method is
    /// the sanctioned way to get an answer without knowing the scheme: the kind applies its own
    /// selection and returns only the resulting token.
    /// </para>
    /// <para>
    /// <b>Opaque, and the only thing a result cache needs.</b> Callers concatenate and compare it,
    /// never parse it. Two callers receiving equal tokens through the same kind may share a cached
    /// result; two receiving different tokens must not. A kind with no session-context concept
    /// returns a constant, so its results cache globally exactly as they did before this existed.
    /// </para>
    /// </remarks>
    /// <param name="authenticationContext">
    /// The authentication context of the current logical call flow, or <see langword="null"/> when
    /// none has been established.
    /// </param>
    string CachePartition(IAuthenticationContext? authenticationContext);

    /// <summary>
    /// Gets the longest a result read through this connection kind may be replayed from cache for a
    /// caller with <paramref name="authenticationContext"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The companion to <see cref="CachePartition"/> on the time axis. The partition separates callers
    /// whose visibility differs now; this bounds how long a result stays valid when visibility can
    /// change without the caller changing — a revoked grant the partition cannot see.
    /// </para>
    /// <para>
    /// A ceiling, never an extension: a consumer caps whatever duration it would otherwise use and
    /// keeps a shorter one. <see cref="TimeSpan.MaxValue"/> means the kind imposes no bound.
    /// </para>
    /// </remarks>
    /// <param name="authenticationContext">
    /// The authentication context of the current logical call flow, or <see langword="null"/> when
    /// none has been established.
    /// </param>
    TimeSpan MaxCacheDuration(IAuthenticationContext? authenticationContext);
}
