using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// CRTP base class for connection limit TypeOptions.
/// Concrete limit types (RateLimit, QueryTimeout, etc.) inherit from this in each
/// connection-type package (MsSql, Http, …).
///
/// The numeric Id is the discriminator within a single connection type's TypeCollection.
/// Ids do NOT need to be globally unique — only unique within one TypeCollection.
/// </summary>
public abstract class ConnectionLimitTypeBase
    : TypeOptionBase<int, ConnectionLimitTypeBase>, IConnectionLimitType
{
    /// <summary>
    /// Constructor for the source-generated Empty/NotFound sentinel.
    /// </summary>
    protected ConnectionLimitTypeBase()
        : base(0, string.Empty)
    {
        ConfigurationFields = [];
    }

    /// <summary>
    /// Constructor for concrete TypeOptions.
    /// </summary>
    /// <param name="id">Unique numeric identifier within the connection type's TypeCollection.</param>
    /// <param name="name">TypeCollection lookup key (e.g. "RateLimit", "QueryTimeout").</param>
    /// <param name="displayName">Human-readable label shown in the UI.</param>
    /// <param name="description">Short description rendered as UI help text.</param>
    /// <param name="configurationFields">Fields rendered by the per-limit-type editor.</param>
    protected ConnectionLimitTypeBase(
        int id,
        string name,
        string displayName,
        string description,
        IReadOnlyList<ConfigurationFieldDescriptor> configurationFields)
        : base(id, name, $"ConnectionLimit:{name}", displayName, description, "ConnectionLimit")
    {
        ConfigurationFields = configurationFields;
    }

    /// <inheritdoc />
    public IReadOnlyList<ConfigurationFieldDescriptor> ConfigurationFields { get; }
}
