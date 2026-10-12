using Fdw.Roslyn.Commands.Abstractions;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Tests.TestDoubles;

/// <summary>
/// A command that declares it restores a snapshot and needs it resolved before translation.
/// </summary>
public sealed class FakeSnapshotAwareCommand : FakeRoslynCommand, ISnapshotRestoringCommand
{
    public Solution? SnapshotSolution { get; set; }

    public string? SnapshotId { get; set; }
}
