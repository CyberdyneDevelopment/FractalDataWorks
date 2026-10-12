using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data;

/// <summary>
/// TypeCollection for which side of a DataSet's lineage a closure read is filtered to.
/// </summary>
[TypeCollection(typeof(LineageClosureDirectionBase), typeof(ILineageClosureDirection), typeof(LineageClosureDirections))]
[ExcludeFromCodeCoverage]
public abstract partial class LineageClosureDirections : TypeCollectionBase<LineageClosureDirectionBase, ILineageClosureDirection> { }
