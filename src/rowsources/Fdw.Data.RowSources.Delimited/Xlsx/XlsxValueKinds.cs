using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>
/// TypeCollection for what a numeric Excel cell holds (Number, DateTime, Time), chosen by the cell's number format.
/// </summary>
/// <remarks>
/// Excel stores a date and a time of day as a plain number; only the cell's number format says which one it is.
/// <c>XlsxValueKinds.ByName(kind).Read(text, date1904)</c> turns the stored text into the value the cell holds, so nothing
/// branches on the kind (FDW-1341).
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(XlsxValueKindBase), typeof(IXlsxValueKind), typeof(XlsxValueKinds))]
public abstract partial class XlsxValueKinds : TypeCollectionBase<XlsxValueKindBase, IXlsxValueKind>
{
}
