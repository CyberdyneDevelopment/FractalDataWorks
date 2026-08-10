using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Base class for data destination kinds.
/// </summary>
public abstract class DataDestinationKindBase : TypeOptionBase<int, DataDestinationKindBase>, IDataDestinationKind
{
    /// <summary>
    /// Initializes a new instance of <see cref="DataDestinationKindBase"/>.
    /// </summary>
    protected DataDestinationKindBase(int id, string name) : base(id, name) { }
}
