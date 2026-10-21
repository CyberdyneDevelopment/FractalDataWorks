namespace Fdw.Services.Messaging;

/// <summary>
/// The area of a screen a UI comment marks, in window coordinates (FDW-2143).
/// </summary>
public sealed class UiFeedbackArea
{
    /// <summary>Gets or sets the distance of the area's left edge from the window's left edge.</summary>
    public double X { get; set; }

    /// <summary>Gets or sets the distance of the area's top edge from the window's top edge.</summary>
    public double Y { get; set; }

    /// <summary>Gets or sets the area's width; not negative.</summary>
    public double W { get; set; }

    /// <summary>Gets or sets the area's height; not negative.</summary>
    public double H { get; set; }
}
