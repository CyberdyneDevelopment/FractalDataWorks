using System;
using System.Collections.Generic;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// Marker interface for ConfigurationSaveCommand&lt;T&gt;.
/// Used by the DataGateway cascade handler to identify configuration save commands
/// without requiring per-type knowledge. Cascade detection via this interface avoids
/// a generic-type constraint on the Execute path and keeps the check O(1).
/// </summary>
/// <remarks>
/// Why: IDataCommand is not a good hook — all commands implement it. A dedicated marker
/// lets the gateway intercept only saves without branching on CommandType string comparison.
/// Extends IDataCommandWithInput so the cascade handler can access the raw data object for
/// per-level ConfigurationSaveCommand construction without reflection or generics.
/// </remarks>
public interface IConfigurationSaveCommand : IDataCommand, IDataCommandWithInput
{
    /// <summary>
    /// Gets the CLR type of the configuration entity being saved.
    /// Used by the DataGateway to identify the configuration record type without a generic type parameter.
    /// </summary>
    Type ConfigurationType { get; }

    /// <summary>Extra column=value pairs merged into the INSERT beyond the POCO's mapped columns
    /// (e.g. a KVP child's logical owner FK). Empty for ordinary saves.</summary>
    IReadOnlyDictionary<string, object?> AdditionalColumnValues { get; }
}
