using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.UI.Abstractions.Charts;

namespace Fdw.UI.Charts.Blazor.Tests.Fakes;

/// <summary>
/// Minimal <see cref="IChartDataSource"/> stub for tests.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class FakeChartDataSource : IChartDataSource
{
    /// <inheritdoc />
    public string DataSetName { get; set; } = "test-dataset";

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string>? QueryParameters { get; set; }

    /// <inheritdoc />
    public int? RowLimit { get; set; }
}
