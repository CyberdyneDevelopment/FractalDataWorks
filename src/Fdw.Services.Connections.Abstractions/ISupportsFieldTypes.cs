using System.Collections.Generic;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// Opt-in capability interface for connection types that declare which field (column) types they support.
/// </summary>
/// <remarks>
/// Connection types implement this interface to enumerate the field types they can store and retrieve.
/// Consistent with the <c>ISupportsCalculationPushdown</c> capability pattern.
/// </remarks>
public interface ISupportsFieldTypes
{
    /// <summary>
    /// Gets the field type descriptors supported by this connection type.
    /// </summary>
    IReadOnlyList<FieldTypeInfo> SupportedFieldTypes { get; }
}
