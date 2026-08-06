using Fdw.Collections;
using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Data.Abstractions;

/// <summary>
/// TypeCollection for all container type implementations.
/// Containers represent data sources like tables, views, API endpoints, files.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ContainerTypeBase), typeof(IContainerType), typeof(ContainerTypes), RestrictToCurrentCompilation = false)]
public sealed partial class ContainerTypes : TypeCollectionBase<ContainerTypeBase, IContainerType>
{
    // TypeCollectionGenerator will generate all members
}
