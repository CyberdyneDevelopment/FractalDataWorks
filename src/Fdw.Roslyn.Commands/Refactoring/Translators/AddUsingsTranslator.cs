using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Refactoring.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
// Why: Fdw.Roslyn.Commands.Compilation namespace now lives in this assembly; alias
// disambiguates the Roslyn Compilation type from the sibling namespace.
using MsCompilation = Microsoft.CodeAnalysis.Compilation;

namespace Fdw.Roslyn.Commands.Refactoring.Translators;

/// <summary>
/// Translator for AddUsingsCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "AddUsings")]
public sealed class AddUsingsTranslator : RoslynCommandTranslatorBase<AddUsingsCommand, MutationResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddUsingsTranslator"/> class.
    /// </summary>
    public AddUsingsTranslator()
        : base("AddUsings", "Adds missing using directives to a file")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: find unresolved types, search assemblies, add using directives
    public override async Task<IGenericResult<MutationResult>> Translate(
        AddUsingsCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));

        var document = solution.GetDocument(documentId);
        if (document is null)
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));

        // Get existing usings
        var existingUsings = syntaxRoot.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Select(u => u.Name?.ToString() ?? string.Empty)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToHashSet(StringComparer.Ordinal);

        // Find all unresolved type references
        var diagnostics = semanticModel.GetDiagnostics(cancellationToken: cancellationToken);
        var missingUsings = new HashSet<string>(StringComparer.Ordinal);

        foreach (var diagnostic in diagnostics)
        {
            // CS0246: The type or namespace name could not be found
            // CS0103: The name does not exist in the current context
            if (!string.Equals(diagnostic.Id, "CS0246", StringComparison.Ordinal) &&
                !string.Equals(diagnostic.Id, "CS0103", StringComparison.Ordinal))
                continue;

            var location = diagnostic.Location;
            if (!location.IsInSource)
                continue;

            var span = location.SourceSpan;
            var node = syntaxRoot.FindNode(span);

            // Try to find the unresolved type name
            var typeName = node switch
            {
                IdentifierNameSyntax id => id.Identifier.Text,
                GenericNameSyntax generic => generic.Identifier.Text,
                QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
                _ => null
            };

            if (typeName is null)
                continue;

            // Search for matching types in referenced assemblies
            var compilation = semanticModel.Compilation;
            var matchingTypes = FindMatchingTypes(compilation, typeName);

            foreach (var matchingType in matchingTypes)
            {
                var namespaceName = matchingType.ContainingNamespace.ToDisplayString();
                if (!existingUsings.Contains(namespaceName))
                {
                    missingUsings.Add(namespaceName);
                }
            }
        }

        // Add missing usings to the document
        var newRoot = syntaxRoot;
        foreach (var namespaceName in missingUsings.OrderBy(n => n, StringComparer.Ordinal))
        {
            var usingDirective = SyntaxFactory.UsingDirective(
                SyntaxFactory.ParseName(namespaceName))
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            newRoot = newRoot.InsertNodesBefore(
                newRoot.DescendantNodes().First(),
                new[] { usingDirective });
        }

        var newDocument = document.WithSyntaxRoot(newRoot);
        var newSolution = newDocument.Project.Solution;

        var fileChanges = new List<FileChange>
        {
            new FileChange(command.FilePath, FileChangeTypes.Modified, document.Project.Name)
            {
                TextChangeCount = missingUsings.Count
            }
        };

        return GenericResult<MutationResult>.Success(
            new MutationResult(
                $"Added {missingUsings.Count} using directives",
                newSolution,
                fileChanges));
    }
#pragma warning restore MA0051

    private static List<INamedTypeSymbol> FindMatchingTypes(MsCompilation compilation, string typeName)
    {
        var results = new List<INamedTypeSymbol>();

        // Search in all referenced assemblies
        foreach (var reference in compilation.References)
        {
            var assemblySymbol = compilation.GetAssemblyOrModuleSymbol(reference) as IAssemblySymbol;
            if (assemblySymbol is null)
                continue;

            SearchNamespace(assemblySymbol.GlobalNamespace, typeName, results);
        }

        // Also search in the compilation's assembly
        SearchNamespace(compilation.Assembly.GlobalNamespace, typeName, results);

        return results;
    }

    private static void SearchNamespace(INamespaceSymbol ns, string typeName, List<INamedTypeSymbol> results)
    {
        foreach (var type in ns.GetTypeMembers())
        {
            if (string.Equals(type.Name, typeName, StringComparison.Ordinal) &&
                type.DeclaredAccessibility == Accessibility.Public)
            {
                results.Add(type);
            }
        }

        foreach (var childNs in ns.GetNamespaceMembers())
        {
            SearchNamespace(childNs, typeName, results);
        }
    }
}
