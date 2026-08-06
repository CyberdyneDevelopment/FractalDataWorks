using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.MsSql.Limits;

/// <summary>
/// TypeCollection of outbound connection limit options for MsSql connections.
/// Mirrors the MsSqlAuthenticationTypes pattern: each entry identifies a limit
/// kind and its UI configuration fields.
///
/// Usage:
///   var limitType = MsSqlConnectionLimitTypes.ByName("RateLimit");
///   if (limitType == MsSqlConnectionLimitTypes.NotFound) { /* unknown type */ }
///   // Render limitType.ConfigurationFields in the UI
///
/// New limit kinds can be added from any loaded assembly via [TypeOption] without
/// framework changes.
/// </summary>
[TypeCollection(
    typeof(ConnectionLimitTypeBase),
    typeof(IConnectionLimitType),
    typeof(MsSqlConnectionLimitTypes))]
public abstract partial class MsSqlConnectionLimitTypes
    : TypeCollectionBase<ConnectionLimitTypeBase, IConnectionLimitType>
{
}
