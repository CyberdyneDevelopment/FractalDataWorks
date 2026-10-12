using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Commands.Development.Abstractions;

/// <summary>
/// Type collection for development command categories.
/// Categories are shared across all language implementations.
/// </summary>
[TypeCollection(typeof(DevelopmentCommandCategoryBase), typeof(IDevelopmentCommandCategory), typeof(DevelopmentCommandCategories))]
public abstract partial class DevelopmentCommandCategories
    : TypeCollectionBase<DevelopmentCommandCategoryBase, IDevelopmentCommandCategory>
{
}
