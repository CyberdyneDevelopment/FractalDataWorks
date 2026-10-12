using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Where a loaded column gets its value: a column of the source row, the next value of a sequence, the current time or a constant (FDW-1108).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ValueSourceKindBase), typeof(IValueSourceKind), typeof(ValueSourceKinds))]
public abstract partial class ValueSourceKinds : TypeCollectionBase<ValueSourceKindBase, IValueSourceKind>
{
    // DO NOT IMPLEMENT BY HAND! Source generator populates ByName/ById/All/NotFound.
}
