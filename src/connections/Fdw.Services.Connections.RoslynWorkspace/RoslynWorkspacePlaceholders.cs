using Fdw.Conventions;

namespace Fdw.Services.Connections.RoslynWorkspace;

/// <summary>
/// The texts the Roslyn workspace connection factory's failure messages show where a value is absent. None of them is
/// ever a real configuration type name.
/// </summary>
/// <remarks>
/// Why declared here: FDW055 accepts a placeholder only as a [PlaceholderText] constant, so the text is named once,
/// with the wording the messages have always used (FDW-3580).
/// </remarks>
[PlaceholderText]
public static class RoslynWorkspacePlaceholders
{
    /// <summary>The factory was handed no configuration at all.</summary>
    public const string NoConfiguration = "null";
}
