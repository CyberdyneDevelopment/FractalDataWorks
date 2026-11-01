namespace Fdw.Calculations.Endpoints;

/// <summary>One window function a windowed calculation may name.</summary>
public sealed class WindowFunctionPayload
{
    /// <summary>Gets or sets the function name a windowed calculation names in its WindowFunction.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the function's category.</summary>
    public string Category { get; set; } = string.Empty;
}
