using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Formatting.Commands;
using Fdw.Roslyn.Commands.Formatting.Results;
using Fdw.Roslyn.Commands.Logging;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Formatting.Translators;

/// <summary>
/// Translator for OrganizeUsingsCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "OrganizeUsings")]
public sealed class OrganizeUsingsTranslator : RoslynCommandTranslatorBase<OrganizeUsingsCommand, MutationResult<OrganizedUsingsData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OrganizeUsingsTranslator"/> class.
    /// </summary>
    public OrganizeUsingsTranslator()
        : base("OrganizeUsings", "Organizes and sorts using directives")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: collect usings, sort, rebuild compilation unit
    public override async Task<IGenericResult<MutationResult<OrganizedUsingsData>>> Translate(
        OrganizeUsingsCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        OrganizeUsingsTranslatorLog.Organizing(Logger, command.FilePath, command.SystemFirst);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            OrganizeUsingsTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<MutationResult<OrganizedUsingsData>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            OrganizeUsingsTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<MutationResult<OrganizedUsingsData>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (syntaxRoot is null)
        {
            OrganizeUsingsTranslatorLog.FailedToGetSyntaxRoot(Logger, command.FilePath);
            return GenericResult<MutationResult<OrganizedUsingsData>>.Failure(
                RoslynResultCodes.ByName("FailedToGetSyntaxRoot"));
        }

        // Get all using directives
        var usings = syntaxRoot.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .ToList();

        if (usings.Count == 0)
        {
            OrganizeUsingsTranslatorLog.NoUsingsToOrganize(Logger, command.FilePath);
            var emptyData = new OrganizedUsingsData
            {
                UsingCount = 0,
                OrganizedUsings = Array.Empty<UsingInfo>()
            };

            return GenericResult<MutationResult<OrganizedUsingsData>>.Success(
                new MutationResult<OrganizedUsingsData>(
                    "No using directives to organize",
                    solution,
                    Array.Empty<FileChange>(),
                    emptyData));
        }

        // Sort usings
        var sortedUsings = usings
            .OrderBy(u => !command.SystemFirst || u.Name?.ToString().StartsWith("System", StringComparison.Ordinal) != true ? 1 : 0)
            .ThenBy(u => u.Name?.ToString() ?? string.Empty, StringComparer.Ordinal)
            .ToList();

        // Build new root with sorted usings
        var newRoot = syntaxRoot;

        // Get the compilation unit
        var compilationUnit = syntaxRoot as CompilationUnitSyntax;
        if (compilationUnit is not null && usings.Count > 0)
        {
            // Remove old usings
            var withoutUsings = compilationUnit.RemoveNodes(usings, SyntaxRemoveOptions.KeepNoTrivia);

            if (withoutUsings is CompilationUnitSyntax newCompilationUnit)
            {
                // Add sorted usings
                var newUsings = SyntaxFactory.List(sortedUsings);
                newRoot = newCompilationUnit.WithUsings(newUsings);
            }
        }

        var newDocument = document.WithSyntaxRoot(newRoot);
        var newSolution = newDocument.Project.Solution;

        // Build organized using list info
        var organizedUsings = sortedUsings.Select(u => new UsingInfo
        {
            Namespace = u.Name?.ToString() ?? string.Empty
        }).ToList();

        var fileChanges = new List<FileChange>();
        if (usings.Count > 0)
        {
            fileChanges.Add(new FileChange(command.FilePath, FileChangeTypes.Modified, document.Project.Name)
            {
                TextChangeCount = usings.Count
            });
        }

        var data = new OrganizedUsingsData
        {
            UsingCount = usings.Count,
            OrganizedUsings = organizedUsings
        };

        OrganizeUsingsTranslatorLog.Organized(Logger, command.FilePath, usings.Count);

        return GenericResult<MutationResult<OrganizedUsingsData>>.Success(
            new MutationResult<OrganizedUsingsData>(
                $"Organized {usings.Count} using directives",
                newSolution,
                fileChanges,
                data));
    }
#pragma warning restore MA0051
}
