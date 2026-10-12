using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.UI.Abstractions.Pages;

/// <summary>
/// TypeCollection for row status values.
/// </summary>
[TypeCollection(typeof(RowStatusBase), typeof(IRowStatus), typeof(RowStatuses))]
[ExcludeFromCodeCoverage]
public abstract partial class RowStatuses : TypeCollectionBase<RowStatusBase, IRowStatus> { }
