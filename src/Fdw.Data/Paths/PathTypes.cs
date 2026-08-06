using Fdw.Collections;
using Fdw.Collections.Attributes;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Data.Abstractions;

/// <summary>
/// TypeCollection for all path type implementations.
/// Paths represent navigation to containers within different data store types.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(PathTypeBase), typeof(IPathType), typeof(PathTypes))]
public sealed partial class PathTypes : TypeCollectionBase<PathTypeBase, IPathType>
{
    // TypeCollectionGenerator will generate all members
}
