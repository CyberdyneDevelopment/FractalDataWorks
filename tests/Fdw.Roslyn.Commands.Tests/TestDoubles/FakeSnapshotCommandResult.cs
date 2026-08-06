using Fdw.Roslyn.Commands.Abstractions;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Tests.TestDoubles;

/// <summary>
/// Data payload shape used by <see cref="FakeSnapshotCommandResult"/> — mirrors the real
/// <c>SnapshotData</c> shape (a public settable <c>SnapshotId</c>) that
/// <see cref="RoslynCommandHandler"/> patches via reflection after CreateSnapshot succeeds.
/// </summary>
public sealed class FakeSnapshotResultData
{
    public string? SnapshotId { get; set; }
}

/// <summary>
/// <see cref="IRoslynCommandResult"/> test double exposing a public <c>Data</c> property whose
/// value has a settable <c>SnapshotId</c>, matching the shape <see cref="RoslynCommandHandler"/>
/// looks for via reflection.
/// </summary>
public sealed class FakeSnapshotCommandResult : IRoslynCommandResult
{
    public string Summary => "ok";

    public bool IsMutation => false;

    public Solution? NewSolution => null;

    public FakeSnapshotResultData Data { get; } = new();
}
