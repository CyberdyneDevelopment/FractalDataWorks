using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Http.Limits;

/// <summary>
/// TypeCollection of outbound connection limit options for Http connections.
/// Mirrors MsSqlConnectionLimitTypes; each entry identifies a limit kind and
/// its UI configuration fields.
///
/// Usage:
///   var limitType = HttpConnectionLimitTypes.ByName("MaxRequestRate");
///   if (limitType == HttpConnectionLimitTypes.NotFound) { /* unknown type */ }
///   // Render limitType.ConfigurationFields in the UI
/// </summary>
[TypeCollection(
    typeof(ConnectionLimitTypeBase),
    typeof(IConnectionLimitType),
    typeof(HttpConnectionLimitTypes))]
public abstract partial class HttpConnectionLimitTypes
    : TypeCollectionBase<ConnectionLimitTypeBase, IConnectionLimitType>
{
}
