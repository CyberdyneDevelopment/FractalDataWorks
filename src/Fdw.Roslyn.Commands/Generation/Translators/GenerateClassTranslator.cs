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
using Fdw.Roslyn.Commands.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
// Why: Fdw.Roslyn.Commands.Project namespace now lives in this assembly; alias
// disambiguates the Roslyn Project type from the sibling namespace.
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Generation.Translators;

/// <summary>
/// Translator for GenerateClassCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "GenerateClass")]
public sealed class GenerateClassTranslator : RoslynCommandTranslatorBase<GenerateClassCommand, MutationResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateClassTranslator"/> class.
    /// </summary>
    public GenerateClassTranslator()
        : base("GenerateClass", "Generates a class from template")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear code generation: validate inputs, build class via StringBuilder, add to solution
    public override async Task<IGenericResult<MutationResult>> Translate(
        GenerateClassCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(command.ClassName))
        {
            GenerateClassTranslatorLog.ClassNameRequired(Logger);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("ClassNameRequired"));
        }

        if (string.IsNullOrEmpty(command.Namespace))
        {
            GenerateClassTranslatorLog.NamespaceRequired(Logger);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("NamespaceRequired"));
        }

        GenerateClassTranslatorLog.Generating(Logger, command.ClassName, command.Namespace);

        var sb = new StringBuilder();

        // Build using statements
        sb.AppendLine("using System;");
        sb.AppendLine();

        // Namespace
        sb.AppendLine($"namespace {command.Namespace};");
        sb.AppendLine();

        // XML documentation
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Represents the {command.ClassName} class.");
        sb.AppendLine("/// </summary>");

        // Class declaration
        var modifiers = new List<string> { "public" };
        if (command.IsSealed)
            modifiers.Add("sealed");
        if (command.IsAbstract)
            modifiers.Add("abstract");

        var inheritance = new List<string>();
        if (!string.IsNullOrEmpty(command.BaseClass))
            inheritance.Add(command.BaseClass);
        if (!string.IsNullOrEmpty(command.Interfaces))
            inheritance.AddRange(command.Interfaces.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        var inheritanceStr = inheritance.Count > 0 ? $" : {string.Join(", ", inheritance)}" : string.Empty;

        sb.AppendLine($"{string.Join(" ", modifiers)} class {command.ClassName}{inheritanceStr}");
        sb.AppendLine("{");
        sb.AppendLine("}");

        var generatedCode = sb.ToString();
        var compilationUnit = SyntaxFactory.ParseCompilationUnit(generatedCode);

        // Determine target project and file
        Project? targetProject = null;
        if (!string.IsNullOrEmpty(command.ProjectName))
        {
            targetProject = solution.Projects.FirstOrDefault(p => string.Equals(p.Name, command.ProjectName, StringComparison.Ordinal));
            if (targetProject is null)
            {
                GenerateClassTranslatorLog.ProjectNotFound(Logger, command.ProjectName);
                return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("ProjectNotFound"),
                ResultDetails.Create().With("ProjectName", command.ProjectName));
            }
        }
        else
        {
            targetProject = solution.Projects.FirstOrDefault();
            if (targetProject is null)
            {
                GenerateClassTranslatorLog.NoProjectsFoundInSolution(Logger);
                return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("NoProjectsFoundInSolution"));
            }
        }

        var fileName = $"{command.ClassName}.cs";
        var filePath = command.FilePath ?? fileName;

        // Check if document already exists
        var existingDocId = targetProject.Documents.FirstOrDefault(d => string.Equals(d.Name, fileName, StringComparison.Ordinal))?.Id;
        Document newDocument;

        if (existingDocId is not null)
        {
            // Update existing document
            var existingDoc = targetProject.GetDocument(existingDocId);
            if (existingDoc is null)
            {
                GenerateClassTranslatorLog.FailedToLoadExistingDocument(Logger, fileName);
                return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToLoadExistingDocument"));
            }

            newDocument = existingDoc.WithSyntaxRoot(compilationUnit);
        }
        else
        {
            // Create new document
            newDocument = targetProject.AddDocument(fileName, SourceText.From(generatedCode), null, filePath);
        }

        var newSolution = newDocument.Project.Solution;

        var fileChanges = new List<FileChange>
        {
            new FileChange(filePath, existingDocId is not null ? FileChangeTypes.Modified : FileChangeTypes.Added, targetProject.Name)
            {
                TextChangeCount = 1
            }
        };

        await Task.CompletedTask.ConfigureAwait(false);

        GenerateClassTranslatorLog.Generated(Logger, command.ClassName, command.Namespace);

        return GenericResult<MutationResult>.Success(
            new MutationResult(
                $"Generated class '{command.ClassName}' in namespace '{command.Namespace}'",
                newSolution,
                fileChanges));
    }
#pragma warning restore MA0051
}
