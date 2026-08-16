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
using Microsoft.CodeAnalysis.Rename;

namespace Fdw.Roslyn.Commands.Formatting.Translators;

/// <summary>
/// Translator for ApplyNamingConventionsCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "ApplyNamingConventions")]
public sealed class ApplyNamingConventionsTranslator : RoslynCommandTranslatorBase<ApplyNamingConventionsCommand, MutationResult<NamingConventionsData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApplyNamingConventionsTranslator"/> class.
    /// </summary>
    public ApplyNamingConventionsTranslator()
        : base("ApplyNamingConventions", "Applies naming conventions to code symbols")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: check fields, public members, async methods for naming
    public override async Task<IGenericResult<MutationResult<NamingConventionsData>>> Translate(
        ApplyNamingConventionsCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        ApplyNamingConventionsTranslatorLog.Checking(Logger, command.FilePath, command.UseAsyncSuffix);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            ApplyNamingConventionsTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<MutationResult<NamingConventionsData>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            ApplyNamingConventionsTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<MutationResult<NamingConventionsData>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
        {
            ApplyNamingConventionsTranslatorLog.FailedToAnalyzeDocument(Logger, command.FilePath);
            return GenericResult<MutationResult<NamingConventionsData>>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));
        }

        var violations = new List<NamingViolation>();
        var currentSolution = solution;

        // Check private fields
        var fields = syntaxRoot.DescendantNodes()
            .OfType<FieldDeclarationSyntax>()
            .Where(f => f.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)) ||
                       !f.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) ||
                                            m.IsKind(SyntaxKind.ProtectedKeyword) ||
                                            m.IsKind(SyntaxKind.InternalKeyword)));

        foreach (var field in fields)
        {
            foreach (var variable in field.Declaration.Variables)
            {
                var name = variable.Identifier.Text;
                if (!name.StartsWith(command.PrivateFieldPrefix, StringComparison.Ordinal))
                {
                    var suggested = command.PrivateFieldPrefix + char.ToUpperInvariant(name[0]) + name.Substring(1);
                    if (name.Length > 0 && char.IsLower(name[0]))
                        suggested = command.PrivateFieldPrefix + char.ToUpperInvariant(name[0]) + name.Substring(1);
                    else
                        suggested = command.PrivateFieldPrefix + name;

                    violations.Add(new NamingViolation
                    {
                        SymbolName = name,
                        Kind = "PrivateField",
                        Issue = $"Should start with '{command.PrivateFieldPrefix}'",
                        SuggestedName = suggested,
                        Line = variable.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }
        }

        // Check public members for PascalCase
        var publicMembers = syntaxRoot.DescendantNodes()
            .OfType<MemberDeclarationSyntax>()
            .Where(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)));

        foreach (var member in publicMembers)
        {
            string? name = member switch
            {
                MethodDeclarationSyntax method => method.Identifier.Text,
                PropertyDeclarationSyntax prop => prop.Identifier.Text,
                EventDeclarationSyntax evt => evt.Identifier.Text,
                _ => null
            };

            if (name is not null && name.Length > 0 && char.IsLower(name[0]))
            {
                violations.Add(new NamingViolation
                {
                    SymbolName = name,
                    Kind = "PublicMember",
                    Issue = "Should use PascalCase",
                    SuggestedName = char.ToUpperInvariant(name[0]) + name.Substring(1),
                    Line = member.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                });
            }
        }

        // Check async methods if useAsyncSuffix is enabled
        if (command.UseAsyncSuffix)
        {
            var asyncMethods = syntaxRoot.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.AsyncKeyword)));

            foreach (var method in asyncMethods)
            {
                var name = method.Identifier.Text;
                if (!name.EndsWith("Async", StringComparison.Ordinal))
                {
                    violations.Add(new NamingViolation
                    {
                        SymbolName = name,
                        Kind = "AsyncMethod",
                        Issue = "Should end with 'Async' suffix",
                        SuggestedName = name + "Async",
                        Line = method.GetLocation().GetLineSpan().StartLinePosition.Line + 1
                    });
                }
            }
        }

        var fileChanges = new List<FileChange>();
        if (violations.Count > 0)
        {
            fileChanges.Add(new FileChange(command.FilePath, FileChangeTypes.Modified, document.Project.Name)
            {
                TextChangeCount = violations.Count
            });
        }

        var data = new NamingConventionsData
        {
            Violations = violations
        };

        ApplyNamingConventionsTranslatorLog.Checked(Logger, command.FilePath, violations.Count);

        return GenericResult<MutationResult<NamingConventionsData>>.Success(
            new MutationResult<NamingConventionsData>(
                $"Found {violations.Count} naming convention violations",
                currentSolution,
                fileChanges,
                data));
    }
#pragma warning restore MA0051
}
