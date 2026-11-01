using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions.Authentication;

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
