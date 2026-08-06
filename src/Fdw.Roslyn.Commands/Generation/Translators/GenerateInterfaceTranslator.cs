#pragma warning disable CA1305 // Specify IFormatProvider - code generation uses invariant strings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Generation.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
// Why: Fdw.Roslyn.Commands.Project namespace now lives in this assembly; alias
// disambiguates the Roslyn Project type from the sibling namespace.
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Generation.Translators;

/// <summary>
/// Translator for GenerateInterfaceCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "GenerateInterface")]
public sealed class GenerateInterfaceTranslator : RoslynCommandTranslatorBase<GenerateInterfaceCommand, MutationResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateInterfaceTranslator"/> class.
    /// </summary>
    public GenerateInterfaceTranslator()
        : base("GenerateInterface", "Generates an interface")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear code generation: validate inputs, build interface via StringBuilder
    public override async Task<IGenericResult<MutationResult>> Translate(
        GenerateInterfaceCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(command.InterfaceName))
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("InterfaceNameRequired"));

        if (string.IsNullOrEmpty(command.Namespace))
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("NamespaceRequired"));

        var methods = string.IsNullOrEmpty(command.Methods)
            ? Array.Empty<string>()
            : command.Methods.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var properties = string.IsNullOrEmpty(command.Properties)
            ? Array.Empty<string>()
            : command.Properties.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var sb = new StringBuilder();

        // Build using statements
        sb.AppendLine("using System;");
        sb.AppendLine();

        // Namespace
        sb.AppendLine($"namespace {command.Namespace};");
        sb.AppendLine();

        // XML documentation
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Defines the contract for {command.InterfaceName}.");
        sb.AppendLine("/// </summary>");

        // Interface declaration
        sb.AppendLine($"public interface {command.InterfaceName}");
        sb.AppendLine("{");

        // Properties
        foreach (var prop in properties)
        {
            sb.AppendLine($"    {prop}");
        }

        if (properties.Length > 0 && methods.Length > 0)
            sb.AppendLine();

        // Methods
        foreach (var method in methods)
        {
            sb.AppendLine($"    {method}");
        }

        sb.AppendLine("}");

        var generatedCode = sb.ToString();
        var compilationUnit = SyntaxFactory.ParseCompilationUnit(generatedCode);

        // Determine target project and file
        Project? targetProject = null;
        if (!string.IsNullOrEmpty(command.ProjectName))
        {
            targetProject = solution.Projects.FirstOrDefault(p => string.Equals(p.Name, command.ProjectName, StringComparison.Ordinal));
            if (targetProject is null)
                return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("ProjectNotFound"),
                ResultDetails.Create().With("ProjectName", command.ProjectName));
        }
        else
        {
            targetProject = solution.Projects.FirstOrDefault();
            if (targetProject is null)
                return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("NoProjectsFoundInSolution"));
        }

        var fileName = $"{command.InterfaceName}.cs";
        var filePath = command.FilePath ?? fileName;

        // Check if document already exists
        var existingDocId = targetProject.Documents.FirstOrDefault(d => string.Equals(d.Name, fileName, StringComparison.Ordinal))?.Id;
        Document newDocument;

        if (existingDocId is not null)
        {
            // Update existing document
            var existingDoc = targetProject.GetDocument(existingDocId);
            if (existingDoc is null)
                return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToLoadExistingDocument"));

            newDocument = existingDoc.WithSyntaxRoot(compilationUnit);
        }
        else
        {
            // Create new document
            newDocument = targetProject.AddDocument(fileName, SourceText.From(generatedCode), null, filePath);
        }

        var newSolution = newDocument.Project.Solution;

        var memberCount = methods.Length + properties.Length;

        var fileChanges = new List<FileChange>
        {
            new FileChange(filePath, existingDocId is not null ? FileChangeTypes.Modified : FileChangeTypes.Added, targetProject.Name)
            {
                TextChangeCount = 1
            }
        };

        await Task.CompletedTask.ConfigureAwait(false);

        return GenericResult<MutationResult>.Success(
            new MutationResult(
                $"Generated interface '{command.InterfaceName}' with {memberCount} members",
                newSolution,
                fileChanges));
    }
#pragma warning restore MA0051
}
