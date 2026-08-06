using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// TypeCollection for data destination kinds.
/// </summary>
[TypeCollection(typeof(DataDestinationKindBase), typeof(IDataDestinationKind), typeof(DataDestinationKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class DataDestinationKinds : TypeCollectionBase<DataDestinationKindBase, IDataDestinationKind> { }
