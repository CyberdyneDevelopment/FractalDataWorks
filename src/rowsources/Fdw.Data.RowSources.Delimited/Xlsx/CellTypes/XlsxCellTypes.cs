using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>The worksheet cell types, each found by the code a cell's t attribute carries. A cell with no t attribute is a number.</summary>
/// <remarks>A code this collection does not hold answers the NotFound sentinel; the reader refuses the cell.</remarks>
[TypeCollection(typeof(XlsxCellTypeBase), typeof(IXlsxCellType), typeof(XlsxCellTypes))]
[ExcludeFromCodeCoverage]
public abstract partial class XlsxCellTypes : TypeCollectionBase<XlsxCellTypeBase, IXlsxCellType>
{
}
