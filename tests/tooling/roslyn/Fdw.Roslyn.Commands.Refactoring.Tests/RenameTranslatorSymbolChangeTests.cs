using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Refactoring.Commands;
using Fdw.Roslyn.Commands.Refactoring.Translators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Refactoring.Tests;

/// <summary>
/// Unit tests verifying <see cref="RenameTranslator"/> records a <see cref="SymbolChange"/>
/// with exact fully-qualified names for global-namespace, namespaced, and member symbols.
/// </summary>
public sealed class RenameTranslatorSymbolChangeTests
{
    private static readonly Lazy<IReadOnlyList<MetadataReference>> References = new(() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .ToList());

    private static Solution NewSolution(string source, string filePath)
    {
        var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var projectInfo = ProjectInfo.Create(projectId, VersionStamp.Create(), "TestProject", "TestProject", LanguageNames.CSharp)
            .WithMetadataReferences(References.Value)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return workspace.CurrentSolution
            .AddProject(projectInfo)
            .AddDocument(DocumentId.CreateNewId(projectId), Path.GetFileName(filePath), SourceText.From(source), filePath: filePath);
    }

    private static async Task<SymbolChange> RenameAndGetSymbolChange(
        string source, string filePath, int line, int column, string newName)
    {
        var command = new RenameCommand
        {
            FilePath = filePath,
            Line = line,
            Column = column,
            NewName = newName
        };
        var translator = new RenameTranslator();

        var result = await translator.Translate(command, NewSolution(source, filePath), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        var mutation = result.Value.ShouldNotBeNull();
        mutation.SymbolChanges.Count.ShouldBe(1);
        return mutation.SymbolChanges[0];
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TranslateRecordsExactBareNamesForGlobalNamespaceClass()
    {
        var symbolChange = await RenameAndGetSymbolChange(
            "public class Foo\n{\n}\n", "/virtual/Foo.cs", line: 1, column: 14, newName: "Bar");

        symbolChange.ChangeType.ShouldBe(SymbolChangeTypes.Renamed.Name);
        symbolChange.SymbolKind.ShouldBe("NamedType");
        symbolChange.OldFullyQualifiedName.ShouldBe("Foo");
        symbolChange.NewFullyQualifiedName.ShouldBe("Bar");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TranslateRecordsExactNamespaceQualifiedNamesForNamespacedClass()
    {
        var symbolChange = await RenameAndGetSymbolChange(
            "namespace Smoke;\n\npublic class Foo\n{\n}\n", "/virtual/Foo.cs", line: 3, column: 14, newName: "Bar");

        symbolChange.ChangeType.ShouldBe(SymbolChangeTypes.Renamed.Name);
        symbolChange.SymbolKind.ShouldBe("NamedType");
        symbolChange.OldFullyQualifiedName.ShouldBe("Smoke.Foo");
        symbolChange.NewFullyQualifiedName.ShouldBe("Smoke.Bar");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "CoreFramework")]
    public async Task TranslateRecordsContainingTypeQualifiedNamesForMethodRename()
    {
        var symbolChange = await RenameAndGetSymbolChange(
            "namespace N;\n\npublic class C\n{\n    public void M()\n    {\n    }\n}\n",
            "/virtual/C.cs", line: 5, column: 17, newName: "Renamed");

        symbolChange.ChangeType.ShouldBe(SymbolChangeTypes.Renamed.Name);
        symbolChange.SymbolKind.ShouldBe("Method");
        symbolChange.OldFullyQualifiedName.ShouldBe("N.C.M");
        symbolChange.NewFullyQualifiedName.ShouldBe("N.C.Renamed");
    }
}
