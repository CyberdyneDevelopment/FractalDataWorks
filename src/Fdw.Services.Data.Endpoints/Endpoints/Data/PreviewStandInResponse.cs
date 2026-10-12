using System.Collections.Generic;

namespace Fdw.Services.Data.Endpoints;

/// <summary>Example values a stand-in strategy would generate.</summary>
public class PreviewStandInResponse
{
    /// <summary>Gets or sets the strategy that generated these values.</summary>
    public string Strategy { get; set; } = string.Empty;

    /// <summary>Gets or sets the generated values, in the order generated. A null entry is a generated null.</summary>
    public IReadOnlyList<string?> Values { get; set; } = [];
}
