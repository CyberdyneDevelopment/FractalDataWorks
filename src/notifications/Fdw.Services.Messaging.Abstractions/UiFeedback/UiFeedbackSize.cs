namespace Fdw.Services.Messaging;

/// <summary>
/// The size of the window a UI comment was left in (FDW-2143).
/// </summary>
public sealed class UiFeedbackSize
{
    /// <summary>Gets or sets the window's width in pixels; more than zero.</summary>
    public int W { get; set; }

    /// <summary>Gets or sets the window's height in pixels; more than zero.</summary>
    public int H { get; set; }
}
