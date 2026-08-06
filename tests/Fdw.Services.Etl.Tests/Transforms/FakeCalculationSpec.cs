using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl.Tests.Transforms;

/// <summary>
/// Minimal <see cref="ICalculationSpec"/> test double for <c>CalculateTransformType.MapSpecToConfiguration</c> tests.
/// </summary>
internal sealed class FakeCalculationSpec : ICalculationSpec
{
    /// <inheritdoc />
    public string OutputField { get; set; } = string.Empty;

    /// <inheritdoc />
    public string Formula { get; set; } = string.Empty;

    /// <inheritdoc />
    public string FormulaLanguage { get; set; } = string.Empty;
}
