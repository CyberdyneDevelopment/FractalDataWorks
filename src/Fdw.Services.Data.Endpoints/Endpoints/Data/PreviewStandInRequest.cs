namespace Fdw.Services.Data.Endpoints;

/// <summary>Generates example values for a stand-in strategy without persisting it.</summary>
/// <remarks>Carries the same fields as <see cref="SetDataSetFieldStandInRequest"/> so a caller can
/// try a strategy before committing to it; inherits rather than duplicates for exactly that reason.</remarks>
public class PreviewStandInRequest : SetDataSetFieldStandInRequest
{
    /// <summary>Gets or sets how many example values to generate. Defaults to 5.</summary>
    public int Count { get; set; } = 5;
}
