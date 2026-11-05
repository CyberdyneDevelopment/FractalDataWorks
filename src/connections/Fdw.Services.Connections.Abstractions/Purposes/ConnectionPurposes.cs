using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Abstractions.Purposes;

/// <summary>The purposes a connection is granted and assigned for (FDW-3673). A grant naming any other purpose is refused.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ConnectionPurposeBase), typeof(IConnectionPurpose), typeof(ConnectionPurposes))]
public abstract partial class ConnectionPurposes : TypeCollectionBase<ConnectionPurposeBase, IConnectionPurpose>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
