using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Protocols.Tds;

/// <summary>The widths an INTN value is written in (MS-TDS 2.2.5.5.1.1): one, two, four or eight bytes, found by <c>ByWidth</c>.</summary>
/// <remarks>A width no option holds is refused by <see cref="TdsValueReader"/> with the reason, never read as something near it.</remarks>
[TypeCollection(typeof(TdsIntegerWidthBase), typeof(ITdsIntegerWidth), typeof(TdsIntegerWidths))]
[ExcludeFromCodeCoverage]
public abstract partial class TdsIntegerWidths : TypeCollectionBase<TdsIntegerWidthBase, ITdsIntegerWidth>
{
}
