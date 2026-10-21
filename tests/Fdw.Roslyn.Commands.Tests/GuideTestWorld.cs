using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Builds the in-memory solution and the on-disk guides folder the implementation-guide tests run against.
/// </summary>
/// <remarks>
/// The solution has three projects: <c>Acme.Contracts</c> declares the abstraction a guide applies to,
/// <c>Acme.Impl</c> references it and declares a type that leaves a member unimplemented, and
/// <c>Acme.Unrelated</c> references nothing. A fourth, <c>Acme.Legacy.Registration</c>, is what a guide forbids.
/// </remarks>
internal static class GuideTestWorld
{
    internal const string Root = "/world";

    internal const string ContractsSource = @"
namespace Acme.Contracts
{
    /// <summary>A widget.</summary>
    public interface IWidget
    {
        /// <summary>Runs the widget.</summary>
        void Run();

        /// <summary>Gets the widget name.</summary>
        string Name { get; }
    }
}
";

    internal const string ImplSource = @"
namespace Acme.Impl
{
    /// <summary>The widget to copy.</summary>
    public sealed class Gadget : Acme.Contracts.IWidget
    {
        public string Name => ""gadget"";
    }

    public sealed class Complete : Acme.Contracts.IWidget
    {
        public string Name => ""complete"";
        public void Run() { }
    }
}
";

    internal static Solution Build(params string[] referencesFromImpl)
    {
        // Why not disposed: the returned solution is built on this workspace's services, and a disposed
        // workspace can no longer build compilations for it. NamespaceReconcileTestSolution does the same.
        var workspace = new AdhocWorkspace();
        var solution = workspace.AddSolution(
            SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create(), filePath: Root + "/world.slnx"));

        var contracts = ProjectId.CreateNewId();
        var legacy = ProjectId.CreateNewId();
        var impl = ProjectId.CreateNewId();
        var unrelated = ProjectId.CreateNewId();

        solution = Add(solution, contracts, "Acme.Contracts", "IWidget.cs", ContractsSource, Array.Empty<ProjectId>());
        solution = Add(solution, legacy, "Acme.Legacy.Registration", "Legacy.cs", "namespace Acme.Legacy { public sealed class L {} }", Array.Empty<ProjectId>());

        var implRefs = new List<ProjectId> { contracts };
        if (referencesFromImpl.Contains("legacy", StringComparer.Ordinal)) implRefs.Add(legacy);
        solution = Add(solution, impl, "Acme.Impl", "Gadget.cs", ImplSource, implRefs);
        solution = Add(solution, unrelated, "Acme.Unrelated", "Other.cs", "namespace Acme.Unrelated { public sealed class O {} }", Array.Empty<ProjectId>());
        return solution;
    }

    private static Solution Add(
        Solution solution,
        ProjectId id,
        string name,
        string file,
        string source,
        IEnumerable<ProjectId> references)
    {
        var path = Path.Combine(Root, "src", name, file);
        var info = ProjectInfo.Create(
            id,
            VersionStamp.Create(),
            name,
            name,
            LanguageNames.CSharp,
            filePath: Path.Combine(Root, "src", name, name + ".csproj"),
            compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
            parseOptions: new CSharpParseOptions(documentationMode: DocumentationMode.Parse),
            metadataReferences: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            projectReferences: references.Select(r => new ProjectReference(r)));

        return solution
            .AddProject(info)
            .AddDocument(DocumentId.CreateNewId(id), file, SourceText.From(source), filePath: path);
    }

    /// <summary>Writes a guides folder holding one guide and returns its path.</summary>
    internal static string GuidesFolder(Action<Dictionary<string, object?>>? tweak = null)
    {
        var folder = Path.Combine(Path.GetTempPath(), "fdw-guides-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);

        var guide = new Dictionary<string, object?>
        {
            ["implements"] = new[] { new { type = "Acme.Contracts.IWidget", cite = "acme/src/IWidget.cs:5" } },
            ["exemplars"] = new[] { new { type = "Acme.Impl.Gadget", role = "the one to copy", cite = "world/src/Acme.Impl/Gadget.cs:5" } },
            ["references"] = new Dictionary<string, object?>
            {
                ["require"] = new[] { new { assembly = "Acme.Contracts", projects = "Acme.Impl", why = "the contract lives there", cite = "acme/Impl.csproj:3" } },
                ["forbid"] = new[] { new { assembly = "*.Registration", projects = "Acme.*", @except = "*.Registration", why = "an implementation never references a registration", cite = "acme/Impl.csproj:9" } }
            },
            ["must"] = new[] { new { head = "Implements Run", body = "<p>Body.</p>", cite = "acme/src/IWidget.cs:7" } },
            ["mustNot"] = new[] { new { head = "No registration reference", body = "<p>Body.</p>", cite = "acme/Impl.csproj:9" } },
            ["see"] = new[] { new { doc = "registration", page = "phases", why = "phases" } }
        };
        tweak?.Invoke(guide);

        var pages = new[]
        {
            new Dictionary<string, object?>
            {
                ["id"] = "widget", ["label"] = "Widget", ["title"] = "Implementing a widget", ["guide"] = guide
            }
        };
        File.WriteAllText(Path.Combine(folder, "implementing.json"), JsonSerializer.Serialize(pages));
        File.WriteAllText(
            Path.Combine(folder, "implementing.meta.json"),
            JsonSerializer.Serialize(new { repo = "acme", commit = "abc123", verified = "2026-10-13", @ref = "internal" }));
        return folder;
    }
}
