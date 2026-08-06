using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.RoslynWorkspace.Abstractions;

/// <summary>
/// Snapshot mode — the workspace loads on first command and disposes immediately after.
/// Suitable for batch/one-shot analysis where resident memory must be kept low.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RoslynWorkspaceModes), "Snapshot", RestrictToCurrentCompilation = true)]
public sealed class SnapshotMode : RoslynWorkspaceModeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SnapshotMode"/> class.
    /// </summary>
    public SnapshotMode() : base(2, "Snapshot")
    {
    }
}
