using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// TypeCollection for column text alignment.
/// </summary>
[TypeCollection(typeof(ColumnAlignmentBase), typeof(IColumnAlignment), typeof(ColumnAlignments))]
[ExcludeFromCodeCoverage]
public abstract partial class ColumnAlignments : TypeCollectionBase<ColumnAlignmentBase, IColumnAlignment> { }
