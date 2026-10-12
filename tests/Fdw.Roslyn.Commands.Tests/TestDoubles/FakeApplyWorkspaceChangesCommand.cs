using Fdw.Roslyn.Commands.Abstractions;

namespace Fdw.Roslyn.Commands.Tests.TestDoubles;

/// <summary>
/// A command that declares it commits pending changes to disk.
/// </summary>
public sealed class FakeApplyWorkspaceChangesCommand : FakeRoslynCommand, IWorkspaceCommitCommand
{
    public FakeApplyWorkspaceChangesCommand()
    {
        Name = "ApplyWorkspaceChanges";
    }

    public bool DeleteRemovedFiles { get; set; }
}
