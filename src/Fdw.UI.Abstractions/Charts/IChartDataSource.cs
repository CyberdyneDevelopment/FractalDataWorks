using System.Collections.Generic;

namespace Fdw.UI.Abstractions.Charts;

/// <summary>
/// Identifies the data source for a chart tile and carries optional query parameters.
/// </summary>
/// <remarks>
/// <para>
/// This is a lightweight descriptor — it names a dataset and carries filter/sort/paging hints
/// for the renderer or the data layer above it to act on. The chart contract layer does not
/// execute queries; it only carries the descriptor.
/// </para>
/// <para>
/// No data-layer types (DataGateway, IDataStore, IDataSet) appear here — the chart contract
/// is render-agnostic and has no dependency on the data domain packages.
/// </para>
/// </remarks>
public interface IChartDataSource
{
    /// <summary>
    /// Gets the logical name of the dataset that provides rows for this chart.
    /// </summary>
    string DataSetName { get; }

    /// <summary>
    /// Gets an optional query descriptor carrying filter predicates, sort order, and paging
    /// for the data request. Null means "return all rows with no filter".
    /// </summary>
    /// <remarks>
    /// The descriptor is opaque to the chart contract layer. The renderer or a chart data
    /// provider interprets the entries — the contract layer does not parse them.
    /// </remarks>
    IReadOnlyDictionary<string, string>? QueryParameters { get; }

    /// <summary>
    /// Gets an optional row limit applied before the data reaches the renderer.
    /// </summary>
    /// <remarks>
    /// Null means no limit. The renderer may impose its own performance limit regardless of
    /// this value.
    /// </remarks>
    int? RowLimit { get; }
}
