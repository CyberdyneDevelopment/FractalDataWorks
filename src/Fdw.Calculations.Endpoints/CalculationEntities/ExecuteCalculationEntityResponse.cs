using System.Diagnostics.CodeAnalysis;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Response from executing a calculation entity.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ExecuteCalculationEntityResponse
{
    /// <summary>Gets or sets the calculation entity name.</summary>
    public string CalculationName { get; set; } = string.Empty;

    /// <summary>Gets or sets the serialized result.</summary>
    public string ResultJson { get; set; } = string.Empty;

    /// <summary>Gets or sets the execution duration in milliseconds.</summary>
    public long DurationMs { get; set; }
}
