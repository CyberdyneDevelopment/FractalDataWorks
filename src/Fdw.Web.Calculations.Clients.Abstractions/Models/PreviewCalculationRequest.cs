namespace Fdw.Web.Calculations.Clients.Models;

/// <summary>
/// Request to preview a calculation with generated sample data.
/// </summary>
public sealed class PreviewCalculationRequest
{
    /// <summary>
    /// Gets or sets the type of calculation to preview.
    /// </summary>
    public string CalculationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of sample data points to generate for the preview.
    /// </summary>
    public int SampleSize { get; set; } = 10;
}
