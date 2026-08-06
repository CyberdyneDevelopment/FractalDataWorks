using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Schema;

/// <summary>
/// Tabular layout - flat rows and columns.
/// </summary>
/// <remarks>
/// <para>
/// Represents data organized in a traditional row/column structure with no nesting.
/// </para>
/// <para>
/// Examples: SQL tables, CSV files, Excel spreadsheets.
/// </para>
/// <para>
/// Characteristics:
/// <list type="bullet">
/// <item>SupportsNesting: false - No hierarchical structures</item>
/// <item>SupportsFlattening: false - Already flat</item>
/// <item>IsTabular: true - Native row/column format</item>
/// </list>
/// </para>
/// </remarks>
[TypeOption(typeof(DataLayouts), "Tabular")]
[ExcludeFromCodeCoverage]
public sealed class TabularLayout : DataLayoutBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TabularLayout"/> class.
    /// </summary>
    public TabularLayout()
        : base(
            id: 1,
            name: "Tabular",
            description: "Flat rows and columns (SQL table, CSV, Excel)",
            supportsNesting: false,
            supportsFlattening: false,
            isTabular: true)
    {
    }
}
