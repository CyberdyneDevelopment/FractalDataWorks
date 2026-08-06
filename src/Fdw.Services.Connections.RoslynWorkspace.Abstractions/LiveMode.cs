using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions;

/// <summary>
/// Live mode — the workspace is kept resident in memory for repeated queries.
/// Suitable for interactive use cases like Navigator.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RoslynWorkspaceModes), "Live", RestrictToCurrentCompilation = true)]
public sealed class LiveMode : RoslynWorkspaceModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LiveMode"/> class.
    /// </summary>
    public LiveMode() : base(1, "Live")
    {
    }
}
