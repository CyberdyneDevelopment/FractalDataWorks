using System;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Analysis.Commands;
using Fdw.Roslyn.Commands.Analysis.Translators;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Refactoring.Commands;
using Fdw.Roslyn.Commands.Refactoring.Results;
using Fdw.Roslyn.Commands.Refactoring.Translators;
using Fdw.Roslyn.Commands.Refactoring.Helpers;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Tests that the preview reports the caller's change rather than the build environment's problems.
/// </summary>
public sealed class ProbeFidelityTests
{
    [Theory]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    [InlineData("/repo/src/Proj/obj/Debug/net10.0/Proj.AssemblyInfo.cs", true)]
    [InlineData("/repo/src/Proj/obj/Debug/netstandard2.0/.NETStandard,Version=v2.0.AssemblyAttributes.cs", true)]
    [InlineData("/repo/src/Proj/obj/Debug/net10.0/EmbeddedAttribute.cs", true)]
    [InlineData("/repo/src/Proj/bin/Release/net10.0/Whatever.cs", true)]
    [InlineData("/repo/src/Proj/Thing.g.cs", true)]
    [InlineData("/repo/src/Proj/Thing.generated.cs", true)]
    [InlineData("/repo/src/Proj/Thing.g.i.cs", true)]
    // Hand-written files whose MSBuild namesakes live under obj/ must NOT be excluded — this repo has
    // 47 hand-written GlobalUsings.cs and several AssemblyInfo.cs carrying [InternalsVisibleTo].
    [InlineData("/repo/src/Proj/GlobalUsings.cs", false)]
    [InlineData("/repo/src/Proj/Properties/AssemblyInfo.cs", false)]
    [InlineData("/repo/src/Proj/Thing.Designer.cs", false)]
    // Real source must never be excluded — including a project whose NAME contains "obj".
    [InlineData("/repo/src/Fdw.Objects/Thing.cs", false)]
    [InlineData("/repo/src/Proj/NativeTypes/BinaryType.cs", false)]
    [InlineData("/repo/src/Proj/Objects/Thing.cs", false)]
    public void GeneratedPathsAreIdentifiedWithoutCatchingRealSource(string path, bool expected) =>
        new FindNamespaceMismatchesCommand().IsGeneratedPath(path).ShouldBe(expected);

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "CoreFramework")]
    public void AnEmptyPathIsNotGenerated() =>
        new FindNamespaceMismatchesCommand().IsGeneratedPath(null).ShouldBeFalse();

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TheFinderDoesNotReportGeneratedFiles()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Services.Connections.MsSql", out var projectId);

        // One genuine mismatch...
        solution = solution.AddType(projectId, "Fdw.Services.Connections.MsSql",
            "NativeTypes/BinaryType.cs", "Fdw.Data.MsSql", "BinaryType");

        // ...and one in obj/, which must be invisible.
        solution = solution.AddType(projectId, "Fdw.Services.Connections.MsSql",
            "obj/Debug/net10.0/EmbeddedAttribute.cs", "Fdw.Data.MsSql", "EmbeddedAttribute");

        var result = await new FindNamespaceMismatchesTranslator().Translate(
            new FindNamespaceMismatchesCommand { IncludeTypes = true },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var report = result.Value.ShouldNotBeNull().Data;

        report.TotalMismatches.ShouldBe(1);
        report.Groups.SelectMany(g => g.Types)
            .ShouldNotContain(t => t.CurrentPath.Contains("/obj/", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task ACompilationWithoutFrameworkReferencesReportsOnceInsteadOfSpewing()
    {
        // A project with NO metadata references at all — every name fails to bind. Without the guard the
        // probe emits a CS0246 per BCL name and buries anything real.
        var workspace = new AdhocWorkspace();
        var solution = workspace.AddSolution(
            SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create(), filePath: "/repo/r.slnx"));

        var projectId = ProjectId.CreateNewId();
        solution = solution.AddProject(ProjectInfo.Create(
            projectId, VersionStamp.Create(), "Fdw.NoRefs", "Fdw.NoRefs",
            LanguageNames.CSharp, filePath: "/repo/src/Fdw.NoRefs/Fdw.NoRefs.csproj"));

        solution = solution.AddDocument(DocumentInfo.Create(
            DocumentId.CreateNewId(projectId), "Thing.cs",
            loader: TextLoader.From(TextAndVersion.Create(
                Microsoft.CodeAnalysis.Text.SourceText.From("""
using System;

namespace Fdw.NoRefs;

public class Thing
{
    private Guid id;
    private System.Threading.CancellationToken token;
}
"""), VersionStamp.Create())),
            filePath: "/repo/src/Fdw.NoRefs/Thing.cs"));

        var findings = await TypeCollisionProbe.Probe(
            solution, new[] { projectId }, new MoveTypeToProjectCommand(), TestContext.Current.CancellationToken);

        // Exactly one honest finding, naming the cause — not a flood of BCL-missing noise.
        var finding = findings.ShouldHaveSingleItem();
        finding.Kind.ShouldBe("ProbeUnavailable");
        finding.Detail.ShouldContain("no framework references");
        findings.ShouldNotContain(f => f.Kind == "TypeCollision");
        findings.ShouldNotContain(f => f.Kind == "UnresolvedReference");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AnUnverifiableProjectFailsAsCannotVerifyNotAsWouldNotCompile()
    {
        // "Nobody can tell whether your change is wrong" must not be reported as "your change is wrong" —
        // that sends the caller hunting for a defect in their edit that may not exist.
        var workspace = new AdhocWorkspace();
        var solution = workspace.AddSolution(
            SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create(), filePath: "/repo/r.slnx"));

        var projectId = ProjectId.CreateNewId();
        solution = solution.AddProject(ProjectInfo.Create(
            projectId, VersionStamp.Create(), "Fdw.NoRefs", "Fdw.NoRefs",
            LanguageNames.CSharp, filePath: "/repo/src/Fdw.NoRefs/Fdw.NoRefs.csproj"));
        solution = solution.AddProject(ProjectInfo.Create(
            ProjectId.CreateNewId(), VersionStamp.Create(), "Fdw.NoRefs.Tests", "Fdw.NoRefs.Tests",
            LanguageNames.CSharp, filePath: "/repo/tests/Fdw.NoRefs.Tests/Fdw.NoRefs.Tests.csproj"));

        solution = solution.AddDocument(DocumentInfo.Create(
            DocumentId.CreateNewId(projectId), "Thing.cs",
            loader: TextLoader.From(TextAndVersion.Create(
                Microsoft.CodeAnalysis.Text.SourceText.From("""
namespace Fdw.Old;

public class Thing
{
    private System.Guid id;
}
"""), VersionStamp.Create())),
            filePath: "/repo/src/Fdw.NoRefs/Thing.cs"));

        var result = await new MoveNamespaceTranslator().Translate(
            new MoveNamespaceCommand { OldNamespace = "Fdw.Old", NewNamespace = "Fdw.New", DryRun = false },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("ChangeCannotBeVerified");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task NamingAGeneratedFileFailsWithTheRealReasonNotZeroMatches()
    {
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Sample", out var projectId);

        solution = solution.AddType(projectId, "Fdw.Sample",
            "obj/Debug/net10.0/Fdw.Sample.AssemblyInfo.cs", "Fdw.Sample.Gen", "Generated");

        var result = await new MoveTypesToNamespaceTranslator().Translate(
            new MoveTypesToNamespaceCommand
            {
                FilePaths = new[] { System.IO.Path.Combine(
                    NamespaceReconcileTestSolution.Root, "src", "Fdw.Sample",
                    "obj/Debug/net10.0/Fdw.Sample.AssemblyInfo.cs") },
                NewNamespace = "Fdw.Sample.Other",
                DryRun = true,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.Code.ShouldNotBeNull().Name.ShouldBe("SelectorMatchedGeneratedFile");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AMovedGenericTypeIsDetectedSoItsConsumerGainsAUsing()
    {
        // GenericNameSyntax is a sibling of IdentifierNameSyntax, so matching only identifiers silently
        // missed every generic type.
        var solution = NamespaceReconcileTestSolution.Empty()
            .AddProject("Fdw.Mcp.Bus", out var projectId);

        solution = solution.AddRaw(projectId, "Fdw.Mcp.Bus", "IHandler.cs", """
namespace Fdw.Mcp.Bus;

public interface IHandler<T>
{
}
""");
        solution = solution.AddRaw(projectId, "Fdw.Mcp.Bus", "Consumer.cs", """
namespace Fdw.Mcp.Bus;

public class Consumer
{
    private IHandler<string>? handler;
}
""");

        var result = await new MoveTypesToNamespaceTranslator().Translate(
            new MoveTypesToNamespaceCommand
            {
                FilePaths = new[] { System.IO.Path.Combine(
                    NamespaceReconcileTestSolution.Root, "src", "Fdw.Mcp.Bus", "IHandler.cs") },
                NewNamespace = "Fdw.Mcp.Bus.Abstractions",
                DryRun = false,
            },
            solution,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var mutation = result.Value.ShouldBeOfType<MutationResult<MoveTypesToNamespaceData>>();

        var consumer = mutation.NewSolution.GetProject(projectId).ShouldNotBeNull()
            .Documents.Single(d => d.Name == "Consumer.cs");
        var text = (await consumer.GetTextAsync(TestContext.Current.CancellationToken)).ToString();

        text.ShouldContain("using Fdw.Mcp.Bus.Abstractions;");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task APreviewReportsAnUnverifiableProjectInsteadOfRefusingIt()
    {
        // A preview writes nothing and cannot break anything, so a refusal there protects nothing while
        // removing the caller's only way to SEE the change. Only a real run — which would write an
        // unchecked rewrite and record it in the ledger as verified — must refuse.
        var workspace = new AdhocWorkspace();
        var solution = workspace.AddSolution(
            SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create(), filePath: "/repo/r.slnx"));

        var projectId = ProjectId.CreateNewId();
        solution = solution.AddProject(Microsoft.CodeAnalysis.ProjectInfo.Create(
            projectId, VersionStamp.Create(), "Fdw.NoRefs", "Fdw.NoRefs",
            LanguageNames.CSharp, filePath: "/repo/src/Fdw.NoRefs/Fdw.NoRefs.csproj"));

        // MoveNamespace refuses unless test projects are loaded, so the blast radius includes them.
        solution = solution.AddProject(Microsoft.CodeAnalysis.ProjectInfo.Create(
            ProjectId.CreateNewId(), VersionStamp.Create(), "Fdw.NoRefs.Tests", "Fdw.NoRefs.Tests",
            LanguageNames.CSharp, filePath: "/repo/tests/Fdw.NoRefs.Tests/Fdw.NoRefs.Tests.csproj"));

        solution = solution.AddDocument(DocumentInfo.Create(
            DocumentId.CreateNewId(projectId), "Thing.cs",
            loader: TextLoader.From(TextAndVersion.Create(
                Microsoft.CodeAnalysis.Text.SourceText.From("namespace Fdw.Old;\n\npublic class Thing\n{\n}\n"),
                VersionStamp.Create())),
            filePath: "/repo/src/Fdw.NoRefs/Thing.cs"));

        var preview = await new MoveNamespaceTranslator().Translate(
            new MoveNamespaceCommand { OldNamespace = "Fdw.Old", NewNamespace = "Fdw.New", DryRun = true },
            solution,
            TestContext.Current.CancellationToken);

        preview.IsSuccess.ShouldBeTrue("a preview must never be refused for unverifiability");

        var real = await new MoveNamespaceTranslator().Translate(
            new MoveNamespaceCommand { OldNamespace = "Fdw.Old", NewNamespace = "Fdw.New", DryRun = false },
            solution,
            TestContext.Current.CancellationToken);

        real.IsSuccess.ShouldBeFalse("a real run must still refuse rather than write an unverified change");
        real.Code.ShouldNotBeNull().Name.ShouldBe("ChangeCannotBeVerified");
    }

    private static Solution UnverifiableSolution()
    {
        var workspace = new AdhocWorkspace();
        var solution = workspace.AddSolution(
            SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create(), filePath: "/repo/r.slnx"));

        var projectId = ProjectId.CreateNewId();
        solution = solution.AddProject(Microsoft.CodeAnalysis.ProjectInfo.Create(
            projectId, VersionStamp.Create(), "Fdw.NoRefs", "Fdw.NoRefs",
            LanguageNames.CSharp, filePath: "/repo/src/Fdw.NoRefs/Fdw.NoRefs.csproj"));

        solution = solution.AddProject(Microsoft.CodeAnalysis.ProjectInfo.Create(
            ProjectId.CreateNewId(), VersionStamp.Create(), "Fdw.NoRefs.Tests", "Fdw.NoRefs.Tests",
            LanguageNames.CSharp, filePath: "/repo/tests/Fdw.NoRefs.Tests/Fdw.NoRefs.Tests.csproj"));

        return solution.AddDocument(DocumentInfo.Create(
            DocumentId.CreateNewId(projectId), "Thing.cs",
            loader: TextLoader.From(TextAndVersion.Create(
                Microsoft.CodeAnalysis.Text.SourceText.From("namespace Fdw.Old;\n\npublic class Thing\n{\n}\n"),
                VersionStamp.Create())),
            filePath: "/repo/src/Fdw.NoRefs/Thing.cs"));
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task AcceptUnverifiedLetsARealRunProceedWhenTheCallerSeesWhatTheProbeCannot()
    {
        var result = await new MoveNamespaceTranslator().Translate(
            new MoveNamespaceCommand
            {
                OldNamespace = "Fdw.Old",
                NewNamespace = "Fdw.New",
                DryRun = false,
                AcceptUnverified = true,
            },
            UnverifiableSolution(),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue("an explicit, reasoned override must be able to proceed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TheOverrideNeedsNothingButItself()
    {
        // Setting the flag IS the deliberate choice; requiring a Reason on top was friction, not safety.
        var result = await new MoveNamespaceTranslator().Translate(
            new MoveNamespaceCommand
            {
                OldNamespace = "Fdw.Old",
                NewNamespace = "Fdw.New",
                DryRun = false,
                AcceptUnverified = true,
            },
            UnverifiableSolution(),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue("the flag alone must be enough to proceed");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task WithoutTheOverrideARealRunStillRefuses()
    {
        var result = await new MoveNamespaceTranslator().Translate(
            new MoveNamespaceCommand { OldNamespace = "Fdw.Old", NewNamespace = "Fdw.New", DryRun = false },
            UnverifiableSolution(),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse("the check must still be on by default");
        result.Code.ShouldNotBeNull().Name.ShouldBe("ChangeCannotBeVerified");
    }
}
