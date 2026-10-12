using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Minimal <see cref="IAggregationSpec"/> test double for <c>AggregateTransformType.MapSpecToConfiguration</c> tests.
/// </summary>
internal sealed class FakeAggregationSpec : IAggregationSpec
{
    /// <inheritdoc />
    public string SourceField { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Function { get; set; } = string.Empty;

    /// <inheritdoc />
    public string OutputField { get; set; } = string.Empty;
}
