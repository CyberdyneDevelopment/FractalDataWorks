using System.Collections.Generic;

namespace Fdw.Calculations.Endpoints;

/// <summary>The window functions a windowed calculation may name.</summary>
public sealed class WindowFunctionsResponse
{
    /// <summary>Gets or sets the offered window functions.</summary>
    public IReadOnlyList<WindowFunctionPayload> Functions { get; set; } = [];
}
