using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Projects.Commands;
using Fdw.Roslyn.Commands.Projects.Translators;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Unit tests verifying <see cref="MoveProjectsTranslator"/> maps its computed move operations
/// onto <c>MutationResult.PathChanges</c>. Uses an <see cref="AdhocWorkspace"/> with synthetic
/// paths — the translator only performs string path computation, no disk I/O.
/// </summary>
public sealed class MoveProjectsTranslatorPathChangeTests
{
    private static Solution NewSolutionWithProject(string solutionPath, string projectPath, string projectName)
    {
        var workspace = new AdhocWorkspace();
        var solutionInfo = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create(), filePath: solutionPath);
        var solution = workspace.AddSolution(solutionInfo);

        var projectInfo = ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            projectName,
            projectName,
            LanguageNames.CSharp,
            filePath: projectPath);

        return solution.AddProject(projectInfo);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TranslateMapsProjectAndSlnxMovesToPathChanges()
    {
        var solution = NewSolutionWithProject("/repo/repo.sln", "/repo/src/foo/Foo.csproj", "Foo");
        var command = new MoveProjectsCommand
        {
            Moves = new[] { new ProjectMoveSpec("Foo", "Bar") }
        };
        var translator = new MoveProjectsTranslator();

        var result = await translator.Translate(command, solution, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var mutation = result.Value.ShouldNotBeNull();

        mutation.PathChanges.ShouldContain(p =>
            p.Kind == "Project" && p.OldPath == "/repo/src/foo" && p.NewPath == "/repo/src/Bar/foo");
        mutation.PathChanges.ShouldContain(p =>
            p.Kind == "SlnxProject" && p.OldPath == "src/foo/Foo.csproj" && p.NewPath == "src/Bar/foo/Foo.csproj");

        mutation.ChangedFiles.ShouldBeEmpty();
        mutation.SymbolChanges.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task TranslateMapsCsprojReferenceRewritesToPathChanges()
    {
        var workspace = new AdhocWorkspace();
        var solutionInfo = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create(), filePath: "/repo/repo.sln");
        var solution = workspace.AddSolution(solutionInfo);

        var fooId = ProjectId.CreateNewId();
        var fooInfo = ProjectInfo.Create(
            fooId, VersionStamp.Create(), "Foo", "Foo", LanguageNames.CSharp, filePath: "/repo/src/foo/Foo.csproj");
        solution = solution.AddProject(fooInfo);

        var barInfo = ProjectInfo.Create(
            ProjectId.CreateNewId(), VersionStamp.Create(), "Bar", "Bar", LanguageNames.CSharp,
            filePath: "/repo/src/bar/Bar.csproj",
            projectReferences: new[] { new ProjectReference(fooId) });
        solution = solution.AddProject(barInfo);

        // Only Foo moves; Bar stays put but its reference path to Foo must be rewritten.
        var command = new MoveProjectsCommand
        {
            Moves = new[] { new ProjectMoveSpec("Foo", "Bar") }
        };
        var translator = new MoveProjectsTranslator();

        var result = await translator.Translate(command, solution, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull().PathChanges.ShouldContain(p => p.Kind == "CsprojReference");
    }
}
