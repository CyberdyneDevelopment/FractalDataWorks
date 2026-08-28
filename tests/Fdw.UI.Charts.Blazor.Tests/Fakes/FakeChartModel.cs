using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.UI.Abstractions.Charts;
using Fdw.UI.Abstractions.RenderModeOptions;
using Moq;

namespace Fdw.UI.Charts.Blazor.Tests.Fakes;

/// <summary>
/// Simple mutable chart model for testing.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class FakeChartModel : IChartModel
{
    private static readonly IRenderMode _viewMode = CreateViewMode();

    private static IRenderMode CreateViewMode()
    {
        var mock = new Mock<IRenderMode>();
        mock.Setup(m => m.Name).Returns("View");
        mock.Setup(m => m.AllowsEditing).Returns(false);
        mock.Setup(m => m.ShowsView).Returns(true);
        return mock.Object;
    }

    /// <inheritdoc />
    public string Id { get; set; } = "test-chart";

    /// <inheritdoc />
    public string Title { get; set; } = "Test Chart";

    /// <inheritdoc />
    public IRenderMode RenderMode { get; set; } = _viewMode;

    /// <inheritdoc />
    public IChartType ChartType { get; set; } = ChartTypes.ByName("Bar");

    /// <inheritdoc />
    public IChartDataSource DataSource { get; set; } = new FakeChartDataSource();

    /// <inheritdoc />
    public IReadOnlyList<ChartEncoding> Encodings { get; set; } = [];

    /// <inheritdoc />
    public string? Subtitle { get; set; }

    /// <inheritdoc />
    public bool ShowXAxisLabel { get; set; }

    /// <inheritdoc />
    public bool ShowYAxisLabel { get; set; }

    /// <inheritdoc />
    public bool ShowLegend { get; set; } = true;

    /// <inheritdoc />
    public bool EnableTooltips { get; set; } = true;

    /// <inheritdoc />
    public bool EnableZoom { get; set; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string>? RendererHints { get; set; }
}
