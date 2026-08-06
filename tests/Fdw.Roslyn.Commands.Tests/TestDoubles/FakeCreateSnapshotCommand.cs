using Fdw.Roslyn.Commands.Abstractions;

namespace Fdw.Roslyn.Commands.Tests.TestDoubles;

/// <summary>
/// A command that declares it creates a snapshot the workspace must store.
/// </summary>
public sealed class FakeCreateSnapshotCommand : FakeRoslynCommand, ISnapshotCreatingCommand
{
    public FakeCreateSnapshotCommand()
    {
        Name = "CreateSnapshot";
    }

    public string SnapshotName { get; set; } = string.Empty;

    public string SnapshotDescription { get; set; } = string.Empty;
}
