using System.Collections.Generic;
using Fdw.UI.Abstractions.Charts;

namespace Fdw.Data.Components.Visualization;

/// <summary>
/// Concrete <see cref="IChartDataSource"/> implementation that carries the dataset name
/// and optional row limit for the Visualize page.
/// </summary>
/// <remarks>
/// Rows are fetched by the Visualize page provider via <c>DataSetApiClient.PreviewDataSet</c>
/// and passed directly to <c>ChartHost.Rows</c>. This descriptor is stored on the
/// <see cref="IChartModel"/> for downstream consumers that read <c>DataSource.DataSetName</c>
/// or <c>DataSource.RowLimit</c> from the model.
/// </remarks>
public sealed class ChartDataSource : IChartDataSource
{
    /// <summary>
    /// Gets the sentinel empty data source used before a dataset is selected.
    /// </summary>
    public static readonly ChartDataSource Empty = new(string.Empty, rowLimit: null);

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartDataSource"/> class.
    /// </summary>
    /// <param name="dataSetName">The logical name of the dataset that provides rows.</param>
    /// <param name="rowLimit">Optional row limit applied before data reaches the renderer.</param>
    public ChartDataSource(string dataSetName, int? rowLimit = null)
    {
        DataSetName = dataSetName;
        RowLimit = rowLimit;
    }

    /// <inheritdoc />
    public string DataSetName { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string>? QueryParameters { get; }

    /// <inheritdoc />
    public int? RowLimit { get; }
}
