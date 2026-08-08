using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Authentication.Abstractions.Security;

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
    /// Gets the command capabilities this connection supports.
    /// The pipeline builder reads this list to populate the capability picker and then renders
    /// the selected capability's <see cref="ICommandCapabilityType.ConfigurationFields"/>
    /// (or its <see cref="ICommandCapabilityType.BuilderComponentType"/> composite widget).
    /// Returns an empty list for connection types that cannot act as a data source or destination
    /// (e.g., notification sinks, health-check endpoints).
    /// </summary>
    IReadOnlyList<ICommandCapabilityType> SupportedCommands { get; }

    /// <summary>
    /// Gets the default response format for containers reached through this transport when a
    /// container declares no explicit <c>Format</c> (e.g. Http → Json). Returns the
    /// <c>FormatTypes.NotFound</c> sentinel for transports that declare none — callers fail loud
    /// rather than inventing a default (no-fallback rule).
    /// </summary>
    IFormatType DefaultResponseFormat { get; }

    /// <summary>
    /// Gets the session contexts this connection kind supports — the ways it can describe the
    /// calling principal to the store it opens. Returns the members of
    /// <see cref="NoSessionContextTypes"/> for kinds that have no session-context concept.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A declared capability surface, at the same status as <see cref="SupportedCommands"/> and
    /// <c>SupportedDiscoveryTypes</c>: it states what the kind can do, for validation, logging and
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
}
