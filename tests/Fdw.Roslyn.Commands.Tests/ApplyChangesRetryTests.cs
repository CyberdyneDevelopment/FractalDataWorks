using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Workspace.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Tests that a failed apply leaves the pending work retryable.
/// </summary>
/// <remarks>
/// The record of what is on disk is the ONLY thing that knows a deletion is still pending, since
/// deletions are found by diffing it against the current solution. Advancing it after a failure
/// discards that knowledge and strands the file permanently.
/// </remarks>
public sealed class ApplyChangesRetryTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "fdw-apply-retry-" + Guid.NewGuid().ToString("N"));

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ARefusedDeleteStaysPendingSoItCanBeRetried()
    {
        Directory.CreateDirectory(_root);
        var filePath = Path.Combine(_root, "Doomed.cs");
        await File.WriteAllTextAsync(filePath, "public class Doomed { }",
            TestContext.Current.CancellationToken);

        var workspace = new AdhocWorkspace();
        var solution = workspace.AddSolution(SolutionInfo.Create(
            SolutionId.CreateNewId(), VersionStamp.Create(), filePath: Path.Combine(_root, "r.slnx")));

        var projectId = ProjectId.CreateNewId();
        solution = solution.AddProject(Microsoft.CodeAnalysis.ProjectInfo.Create(
            projectId, VersionStamp.Create(), "P", "P", LanguageNames.CSharp,
            filePath: Path.Combine(_root, "P.csproj")));

        var documentId = DocumentId.CreateNewId(projectId);
        solution = solution.AddDocument(DocumentInfo.Create(
            documentId, "Doomed.cs",
            loader: TextLoader.From(TextAndVersion.Create(
                SourceText.From("public class Doomed { }"), VersionStamp.Create())),
            filePath: filePath));

        var roslyn = new RoslynWorkspace(solution);

        // The file changes on disk after load, so the delete must be REFUSED rather than destroy the edit.
        await File.WriteAllTextAsync(filePath, "public class Doomed { /* edited by someone else */ }",
            TestContext.Current.CancellationToken);

        roslyn.UpdateSolution(roslyn.CurrentSolution.RemoveDocument(documentId));

        var first = await roslyn.ApplyChanges(deleteRemovedFiles: true, TestContext.Current.CancellationToken);

        first.IsSuccess.ShouldBeFalse();
        File.Exists(filePath).ShouldBeTrue("the delete was correctly refused");

        // The retry is the point: restore the file to its loaded content and apply again. If the record
        // of what is on disk advanced despite the failure, the pending delete is invisible now and this
        // second call silently does nothing — the file is orphaned forever.
        await File.WriteAllTextAsync(filePath, "public class Doomed { }",
            TestContext.Current.CancellationToken);

        var second = await roslyn.ApplyChanges(deleteRemovedFiles: true, TestContext.Current.CancellationToken);

        second.IsSuccess.ShouldBeTrue();
        File.Exists(filePath).ShouldBeFalse("the retry should have completed the deletion");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }
}
