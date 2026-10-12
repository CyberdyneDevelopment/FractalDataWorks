using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Tests;

/// <summary>
/// Builds in-memory solutions for the FDW-595 namespace-reconcile tests.
/// </summary>
/// <remarks>
/// Synthetic paths under an <see cref="AdhocWorkspace"/>. Nothing here touches disk, which is what lets
/// the persistence test assert that a mutation stays in memory.
/// </remarks>
internal static class NamespaceReconcileTestSolution
{
    internal const string Root = "/repo";

    internal static Solution Empty()
    {
        var workspace = new AdhocWorkspace();
        return workspace.AddSolution(
            SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create(), filePath: Root + "/repo.slnx"));
    }

    internal static Solution AddProject(this Solution solution, string name, out ProjectId projectId)
    {
        projectId = ProjectId.CreateNewId();
        return solution.AddProject(ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            name,
            name,
            LanguageNames.CSharp,
            filePath: Path.Combine(Root, "src", name, name + ".csproj"),
            metadataReferences: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            }));
    }

    /// <summary>
    /// Adds a document at <paramref name="relativePath"/> inside the project, declaring
    /// <paramref name="namespaceName"/>.<paramref name="typeName"/>.
    /// </summary>
    internal static Solution AddType(
        this Solution solution,
        ProjectId projectId,
        string projectName,
        string relativePath,
        string namespaceName,
        string typeName,
        bool isTypeOption = false,
        string extraBody = "",
        IEnumerable<string>? usings = null)
    {
        var text = BuildSource(namespaceName, typeName, isTypeOption, extraBody, usings);
        var fullPath = Path.Combine(Root, "src", projectName, relativePath);

        return solution.AddDocument(DocumentInfo.Create(
            DocumentId.CreateNewId(projectId),
            Path.GetFileName(fullPath),
            loader: TextLoader.From(TextAndVersion.Create(SourceText.From(text), VersionStamp.Create())),
            filePath: fullPath));
    }

    /// <summary>Adds a raw document with caller-supplied source.</summary>
    internal static Solution AddRaw(
        this Solution solution,
        ProjectId projectId,
        string projectName,
        string relativePath,
        string source)
    {
        var fullPath = Path.Combine(Root, "src", projectName, relativePath);

        return solution.AddDocument(DocumentInfo.Create(
            DocumentId.CreateNewId(projectId),
            Path.GetFileName(fullPath),
            loader: TextLoader.From(TextAndVersion.Create(SourceText.From(source), VersionStamp.Create())),
            filePath: fullPath));
    }

    private static string BuildSource(
        string namespaceName,
        string typeName,
        bool isTypeOption,
        string extraBody,
        IEnumerable<string>? usings)
    {
        var header = string.Empty;
        if (usings is not null)
        {
            foreach (var u in usings) header += "using " + u + ";" + Environment.NewLine;
        }

        var attribute = isTypeOption
            ? "[TypeOption(typeof(SomeCollection), \"" + typeName + "\")]" + Environment.NewLine
            : string.Empty;

        return header +
               "namespace " + namespaceName + ";" + Environment.NewLine +
               attribute +
               "public class " + typeName + Environment.NewLine +
               "{" + Environment.NewLine +
               extraBody + Environment.NewLine +
               "}" + Environment.NewLine;
    }
}
