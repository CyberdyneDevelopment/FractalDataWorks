using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Refactoring.Commands;
using Fdw.Roslyn.Commands.Refactoring.Results;
using Fdw.Roslyn.Commands.Refactoring.Translators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Tests for <see cref="RemoveGlobalUsingsTranslator"/>.
/// </summary>
/// <remarks>
/// These build a real compilation with corlib so the diagnostic diff — which IS the algorithm — is
/// exercised rather than stubbed. A syntax-only fixture would pass while the command did nothing.
/// </remarks>
public sealed class RemoveGlobalUsingsTests
{
    private static readonly MetadataReference Corlib =
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

    private static (Solution Solution, ProjectId ProjectId) Project(params (string Name, string Text)[] files)
    {
        var workspace = new AdhocWorkspace();
        var solution = workspace.AddSolution(
            SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create(), filePath: "/repo/r.slnx"));

        var projectId = ProjectId.CreateNewId();
        solution = solution.AddProject(ProjectInfo.Create(
                projectId, VersionStamp.Create(), "Fdw.Sample", "Fdw.Sample",
                LanguageNames.CSharp, filePath: "/repo/src/Fdw.Sample/Fdw.Sample.csproj"))
            .AddMetadataReference(projectId, Corlib);

        foreach (var (name, text) in files)
        {
            solution = solution.AddDocument(DocumentInfo.Create(
                DocumentId.CreateNewId(projectId), System.IO.Path.GetFileName(name),
                loader: TextLoader.From(TextAndVersion.Create(SourceText.From(text), VersionStamp.Create())),
                filePath: "/repo/src/Fdw.Sample/" + name));
        }

        return (solution, projectId);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AFileThatReliedOnTheGlobalUsingGainsAnExplicitOne()
    {
        var (solution, projectId) = Project(
            ("GlobalUsings.cs", "global using Fdw.Sample.Lib;\n"),
            ("Lib/Widget.cs", "namespace Fdw.Sample.Lib;\n\npublic class Widget\n{\n}\n"),
            // No using of its own — it resolves Widget purely through the global using.
            ("App/Consumer.cs", "namespace Fdw.Sample.App;\n\npublic class Consumer\n{\n    private Widget? widget;\n}\n"));

        var result = await new RemoveGlobalUsingsTranslator().Translate(
            new RemoveGlobalUsingsCommand
            {
                Project = "Fdw.Sample",
                Namespaces = new[] { "Fdw.Sample.Lib" },
                DryRun = false,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var mutation = result.Value.ShouldBeOfType<MutationResult<RemoveGlobalUsingsData>>();

        mutation.Data.Repaired.ShouldContain(r => r.Contains("Consumer.cs", StringComparison.Ordinal));

        var consumer = mutation.NewSolution.GetProject(projectId).ShouldNotBeNull()
            .Documents.Single(d => d.Name == "Consumer.cs");
        var text = (await consumer.GetTextAsync(TestContext.Current.CancellationToken)).ToString();

        text.ShouldContain("using Fdw.Sample.Lib;");

        var globals = mutation.NewSolution.GetProject(projectId).ShouldNotBeNull()
            .Documents.Single(d => d.Name == "GlobalUsings.cs");
        (await globals.GetTextAsync(TestContext.Current.CancellationToken)).ToString()
            .ShouldNotContain("global using Fdw.Sample.Lib;");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AFileThatNeverUsedTheNamespaceIsLeftAlone()
    {
        // The blast radius is the project, but the repair must be per FILE — blanket insertion would
        // leave imports nothing needs.
        var (solution, projectId) = Project(
            ("GlobalUsings.cs", "global using Fdw.Sample.Lib;\n"),
            ("Lib/Widget.cs", "namespace Fdw.Sample.Lib;\n\npublic class Widget\n{\n}\n"),
            ("App/Consumer.cs", "namespace Fdw.Sample.App;\n\npublic class Consumer\n{\n    private Widget? widget;\n}\n"),
            ("App/Unrelated.cs", "namespace Fdw.Sample.App;\n\npublic class Unrelated\n{\n    private int count;\n}\n"));

        var result = await new RemoveGlobalUsingsTranslator().Translate(
            new RemoveGlobalUsingsCommand
            {
                Project = "Fdw.Sample",
                Namespaces = new[] { "Fdw.Sample.Lib" },
                DryRun = false,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var mutation = result.Value.ShouldBeOfType<MutationResult<RemoveGlobalUsingsData>>();

        var unrelated = mutation.NewSolution.GetProject(projectId).ShouldNotBeNull()
            .Documents.Single(d => d.Name == "Unrelated.cs");
        (await unrelated.GetTextAsync(TestContext.Current.CancellationToken)).ToString()
            .ShouldNotContain("using Fdw.Sample.Lib;");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ANamespaceMsBuildAlsoSuppliesIsRefusedRatherThanSilentlyNoOpped()
    {
        // Deleting the source line changes nothing: the SDK regenerates it next build.
        var (solution, _) = Project(
            ("GlobalUsings.cs", "global using System.Text;\n"),
            ("obj/Debug/net10.0/Fdw.Sample.GlobalUsings.g.cs", "// <auto-generated/>\nglobal using global::System.Text;\n"),
            ("App/Consumer.cs", "namespace Fdw.Sample.App;\n\npublic class Consumer\n{\n}\n"));

        var result = await new RemoveGlobalUsingsTranslator().Translate(
            new RemoveGlobalUsingsCommand
            {
                Project = "Fdw.Sample",
                Namespaces = new[] { "System.Text" },
                DryRun = true,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("GlobalUsingIsMsBuildDuplicate");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task APreviewChangesNothingOnDisk()
    {
        var (solution, _) = Project(
            ("GlobalUsings.cs", "global using Fdw.Sample.Lib;\n"),
            ("Lib/Widget.cs", "namespace Fdw.Sample.Lib;\n\npublic class Widget\n{\n}\n"),
            ("App/Consumer.cs", "namespace Fdw.Sample.App;\n\npublic class Consumer\n{\n    private Widget? widget;\n}\n"));

        var result = await new RemoveGlobalUsingsTranslator().Translate(
            new RemoveGlobalUsingsCommand
            {
                Project = "Fdw.Sample",
                Namespaces = new[] { "Fdw.Sample.Lib" },
                DryRun = true,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();

        // A QueryResult structurally cannot reach the handler's mutation branch or the ledger.
        var query = result.Value.ShouldBeOfType<QueryResult<RemoveGlobalUsingsData>>();
        query.Data.WasDryRun.ShouldBeTrue();
        query.Data.Repaired.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task AnUnknownNamespaceFailsRatherThanReportingSuccess()
    {
        var (solution, _) = Project(
            ("GlobalUsings.cs", "global using Fdw.Sample.Lib;\n"),
            ("Lib/Widget.cs", "namespace Fdw.Sample.Lib;\n\npublic class Widget\n{\n}\n"));

        var result = await new RemoveGlobalUsingsTranslator().Translate(
            new RemoveGlobalUsingsCommand
            {
                Project = "Fdw.Sample",
                Namespaces = new[] { "Fdw.Nope" },
                DryRun = true,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("NoGlobalUsingsMatched");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task AnEmptiedFileIsReportedAndNotDeleted()
    {
        // Deleting a file is a separate, riskier act than editing one, so it is reported for the caller.
        var (solution, projectId) = Project(
            ("GlobalUsings.cs", "global using Fdw.Sample.Lib;\n"),
            ("Lib/Widget.cs", "namespace Fdw.Sample.Lib;\n\npublic class Widget\n{\n}\n"),
            ("App/Consumer.cs", "namespace Fdw.Sample.App;\n\npublic class Consumer\n{\n    private Widget? widget;\n}\n"));

        var result = await new RemoveGlobalUsingsTranslator().Translate(
            new RemoveGlobalUsingsCommand
            {
                Project = "Fdw.Sample",
                Namespaces = new[] { "Fdw.Sample.Lib" },
                DryRun = false,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var mutation = result.Value.ShouldBeOfType<MutationResult<RemoveGlobalUsingsData>>();

        mutation.Data.EmptiedFiles.ShouldContain(f => f.Contains("GlobalUsings.cs", StringComparison.Ordinal));
        mutation.NewSolution.GetProject(projectId).ShouldNotBeNull()
            .Documents.ShouldContain(d => d.Name == "GlobalUsings.cs");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public async Task AFileWithItsOwnExplicitUsingIsNotGivenADuplicate()
    {
        var (solution, projectId) = Project(
            ("GlobalUsings.cs", "global using Fdw.Sample.Lib;\n"),
            ("Lib/Widget.cs", "namespace Fdw.Sample.Lib;\n\npublic class Widget\n{\n}\n"),
            ("App/Consumer.cs", "using Fdw.Sample.Lib;\n\nnamespace Fdw.Sample.App;\n\npublic class Consumer\n{\n    private Widget? widget;\n}\n"));

        var result = await new RemoveGlobalUsingsTranslator().Translate(
            new RemoveGlobalUsingsCommand
            {
                Project = "Fdw.Sample",
                Namespaces = new[] { "Fdw.Sample.Lib" },
                DryRun = false,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var mutation = result.Value.ShouldBeOfType<MutationResult<RemoveGlobalUsingsData>>();

        var consumer = mutation.NewSolution.GetProject(projectId).ShouldNotBeNull()
            .Documents.Single(d => d.Name == "Consumer.cs");
        var text = (await consumer.GetTextAsync(TestContext.Current.CancellationToken)).ToString();

        // Exactly one — a second would be CS0105.
        text.Split("using Fdw.Sample.Lib;").Length.ShouldBe(2);
    }
}
