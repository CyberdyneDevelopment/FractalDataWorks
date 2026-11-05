using Fdw.Conventions;

namespace Fdw.Services.Connections;

/// <summary>
/// The texts the connection service's failure details show where a value is absent. None of them is ever a real type
/// name.
/// </summary>
/// <remarks>
/// Why declared here: FDW055 accepts a placeholder only as a [PlaceholderText] constant, so the text is named once,
/// with the wording the details have always used (FDW-3580).
/// </remarks>
[PlaceholderText]
public static class ConnectionPlaceholders
{
    /// <summary>A successful result that carried no value, named where its type would go.</summary>
    public const string NoValue = "null";
}
