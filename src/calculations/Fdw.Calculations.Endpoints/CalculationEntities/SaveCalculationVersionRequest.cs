using System.Diagnostics.CodeAnalysis;

namespace Fdw.Calculations.Endpoints.CalculationEntities;

/// <summary>
/// Request to save a version of a calculation at a place (FDW-2560). A version carries what the place says about the calculation, its description; the
/// calculation still computes the same way everywhere.
/// </summary>
[ExcludeFromCodeCoverage]
public class SaveCalculationVersionRequest : CalculationVersionPlaceRequest
{
    /// <summary>Gets or sets what the calculation is, in that place's words.</summary>
    public string Description { get; set; } = string.Empty;
}
