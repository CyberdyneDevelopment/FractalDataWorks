using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// One way a connection can discover the schema of the store it opens, or
/// <see cref="NoneSchemaDiscovery"/>: it does not.
/// </summary>
/// <remarks>
/// An option names a strategy; it does not run it. The connection type that declares the option
/// performs the discovery (<c>ISchemaDiscovery</c>), because that is where the services a strategy
/// needs are registered and resolved.
/// </remarks>
public interface ISchemaDiscoveryType : ITypeOption<int, SchemaDiscoveryTypeBase>
{
    /// <summary>
    /// Gets the name a person picks this option by, in a connection form's option picker.
    /// </summary>
    /// <remarks>Implemented by <c>TypeOptionBase.DisplayName</c>; declared here so callers holding the interface can show it.</remarks>
    string DisplayName { get; }

    /// <summary>
    /// Gets whether choosing this option discovers anything: true for every strategy, false for
    /// <see cref="NoneSchemaDiscovery"/>.
    /// </summary>
    /// <remarks>
    /// Why a property of the option: it states what this one member does, so it cannot disagree with
    /// the connection type's declared members or the connection's choice. What a connection type
    /// supports is still read off its declared members, never off a flag beside them.
    /// </remarks>
    bool DiscoversSchema { get; }
}
