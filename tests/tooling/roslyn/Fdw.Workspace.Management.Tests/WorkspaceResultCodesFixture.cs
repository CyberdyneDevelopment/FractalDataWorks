using Fdw.Workspace.Roslyn.Results;
using Xunit;

namespace Fdw.Workspace.Management.Tests;

/// <summary>
/// Fixture that ensures WorkspaceResultCodes TypeCollection is fully initialized
/// before any tests run. This prevents race conditions in the source-generated
/// EnsureFrozen() method when xUnit runs tests in parallel.
/// </summary>
public sealed class WorkspaceResultCodesFixture
{
    public WorkspaceResultCodesFixture()
    {
        _ = WorkspaceResultCodes.All();
    }
}

[CollectionDefinition(nameof(WorkspaceTestCollection))]
public sealed class WorkspaceTestCollection : ICollectionFixture<WorkspaceResultCodesFixture>
{
}
