using System;
using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Connections.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Connections;

/// <summary>
/// The option that declares the defaults of disabling a connection by a threshold (FDW-4192): what the platform allows a rule to do, stated once at a higher level and changed
/// at registration, never hard-coded where it is used.
/// </summary>
/// <remarks>
/// Declared the way the service type options of FDW-3823 declare theirs (<see cref="IDeclaresDefaults"/>): the value is read with <see cref="ServiceTypeDefaults.TryResolve"/>, so a host
/// that wants another answer records it from a Configure body with <c>ServiceTypeDefaults.Override(option, field, value)</c> (this option) or
/// <c>ServiceTypeDefaults.OverrideForCategory("Connection", field, value)</c> (its category). A declared default is strict.
/// </remarks>
public class ConnectionDisablingOption : ITypeOption, IDeclaresDefaults
{
    /// <summary>The name of the default: whether a rule owned by the global (platform) tenant may disable a connection another tenant owns.</summary>
    public const string GlobalRulesMayDisableOtherTenantsConnections = "GlobalRulesMayDisableOtherTenantsConnections";

    private static readonly IReadOnlyDictionary<string, object> Declared =
        ServiceTypeDefaults.With(new Dictionary<string, object>(StringComparer.Ordinal), (GlobalRulesMayDisableOtherTenantsConnections, false));

    /// <summary>Gets the option the connections registration uses.</summary>
    public static ConnectionDisablingOption Instance { get; } = new();

    /// <summary>Initializes a new instance of the <see cref="ConnectionDisablingOption"/> class.</summary>
    public ConnectionDisablingOption()
    {
    }

    /// <inheritdoc/>
    public string Name => "ConnectionDisabling";

    /// <inheritdoc/>
    public string Category => "Connection";

    /// <inheritdoc/>
    public virtual IReadOnlyDictionary<string, object> DeclaredDefaults => Declared;

    /// <summary>Resolves whether a rule owned by the global tenant may disable a connection another tenant owns.</summary>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The answer: the host's override, else the declared default; a failure when neither gives a boolean.</returns>
    public IGenericResult<bool> GlobalRulesMayDisableOthers(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        return ServiceTypeDefaults.TryResolve(this, GlobalRulesMayDisableOtherTenantsConnections, out var value) && value is bool answer
            ? GenericResult<bool>.Success(answer)
            : GenericResult<bool>.Failure(ConnectionDisabledLog.DefaultNotDeclared(logger, GlobalRulesMayDisableOtherTenantsConnections));
    }
}
