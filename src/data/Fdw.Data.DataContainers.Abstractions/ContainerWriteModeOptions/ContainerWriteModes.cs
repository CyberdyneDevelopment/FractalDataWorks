using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataContainers.Abstractions.ContainerWriteModeOptions;

/// <summary>
/// TypeCollection for container write modes.
/// </summary>
/// <remarks>
/// Provides compile-time discovery and O(1) lookup for container write modes.
/// Source generator creates static properties for each registered container write mode.
/// </remarks>
[TypeCollection(typeof(ContainerWriteModeBase), typeof(IContainerWriteMode), typeof(ContainerWriteModes))]
public sealed partial class ContainerWriteModes : TypeCollectionBase<ContainerWriteModeBase, IContainerWriteMode>
{
}
