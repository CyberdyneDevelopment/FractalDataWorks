using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Abstractions;

/// <summary>
/// Type collection for Roslyn command categories.
/// </summary>
[TypeCollection(typeof(RoslynCommandCategoryBase), typeof(IRoslynCommandCategory), typeof(RoslynCommandCategories))]
public abstract partial class RoslynCommandCategories
    : TypeCollectionBase<RoslynCommandCategoryBase, IRoslynCommandCategory>
{
}
