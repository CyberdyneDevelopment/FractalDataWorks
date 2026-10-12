using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Services.Pipelines.Abstractions.DataDestination;

/// <summary>
/// Base class for data destination kind types using CRTP pattern.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class DataDestinationKindBase : TypeOptionBase<DataDestinationKindBase>, IDataDestinationKind
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataDestinationKindBase"/> class.
    /// </summary>
    protected DataDestinationKindBase(string name) : base(name) { }
}
