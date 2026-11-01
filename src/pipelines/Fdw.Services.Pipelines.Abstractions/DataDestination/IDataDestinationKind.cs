using Fdw.Collections;

namespace Fdw.Services.Pipelines.Abstractions.DataDestination;

/// <summary>
/// Interface for data destination kind types.
/// </summary>
public interface IDataDestinationKind : ITypeOption<int, DataDestinationKindBase>
{
}
