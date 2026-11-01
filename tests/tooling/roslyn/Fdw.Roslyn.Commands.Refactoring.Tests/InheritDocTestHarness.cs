using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Refactoring.Commands;
using Fdw.Roslyn.Commands.Refactoring.Results;
using Fdw.Roslyn.Commands.Refactoring.Translators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Refactoring.Tests;

/// <summary>
/// Builds an in-memory <see cref="AdhocWorkspace"/> solution from a single C# source string and runs
/// <see cref="ResolveInheritDocTranslator"/> against it, so the rewriter can be asserted on exact text
/// (including whitespace) without needing MSBuild.
/// </summary>
public static class InheritDocTestHarness
{
    /// <summary>The virtual file path of the single document under test.</summary>
    public const string DocPath = "/virtual/Sample.cs";

    /// <summary>The name of the single project under test.</summary>
    public const string ProjectName = "TestProj";

    private static readonly Lazy<IReadOnlyList<MetadataReference>> References = new(() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .ToList());

    /// <summary>
    /// Runs the translator against <paramref name="source"/> and returns the report plus the rewritten text.
    /// </summary>
    /// <param name="source">The C# source to load as the single document.</param>
    /// <param name="filePath">Optional FilePath scope to pass to the command.</param>
    /// <param name="projectName">Optional ProjectName scope to pass to the command.</param>
    /// <returns>The success flag, the result report, and the document text after rewriting.</returns>
    public static async Task<HarnessRun> RunAsync(string source, string? filePath = null, string? projectName = null)
    {
        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var projectInfo = ProjectInfo.Create(projectId, VersionStamp.Create(), ProjectName, ProjectName, LanguageNames.CSharp)
            .WithMetadataReferences(References.Value)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithParseOptions(new CSharpParseOptions(documentationMode: DocumentationMode.Parse));

        var documentId = DocumentId.CreateNewId(projectId);
        var solution = workspace.CurrentSolution
            .AddProject(projectInfo)
            .AddDocument(documentId, "Sample.cs", SourceText.From(source), filePath: DocPath);

        var command = new ResolveInheritDocCommand();
        if (filePath is not null)
            command.FilePath = filePath;
        if (projectName is not null)
            command.ProjectName = projectName;

        var result = await new ResolveInheritDocTranslator().Translate(command, solution, CancellationToken.None);
        if (!result.IsSuccess)
            return new HarnessRun(false, null, string.Empty);

        var mutation = result.Value!;
        var newText = (await mutation.NewSolution.GetDocument(documentId)!.GetTextAsync()).ToString();
        return new HarnessRun(true, mutation.Data, newText);
    }
}
