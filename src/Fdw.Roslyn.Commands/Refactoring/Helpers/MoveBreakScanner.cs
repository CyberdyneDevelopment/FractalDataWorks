using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Analysis.Helpers;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Refactoring.Results;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Refactoring.Helpers;

/// <summary>
/// Finds everything that breaks when a type moves between assemblies.
/// </summary>
public static class MoveBreakScanner
{
    /// <summary>
    /// Scans the documents being moved for registration hazards.
    /// </summary>
    /// <param name="documents">The documents being moved.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The findings.</returns>
    /// <remarks>
    /// The TypeOption check is the highest-value diagnostic here. A package reference IS a registration in
    /// this codebase: module initializers auto-register every <c>[TypeOption]</c> at assembly load, so
    /// moving one changes which compilation emits its initializer. The build stays perfectly clean and the
    /// TypeCollection is empty at runtime — there is no compiler error to catch it.
    /// </remarks>
    public static async Task<IReadOnlyList<BreakFinding>> ScanMovedDocuments(
        IReadOnlyList<Document> documents,
        CancellationToken cancellationToken = default)
    {
        var findings = new List<BreakFinding>();

        foreach (var document in documents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var declarations = await TypeDeclarationReader.Read(document, cancellationToken).ConfigureAwait(false);
            foreach (var declaration in declarations.Where(d => d.IsTypeOption))
            {
                findings.Add(new BreakFinding
                {
                    Kind = "TypeOptionRegistrationMoves",
                    FilePath = document.FilePath ?? string.Empty,
                    Severity = "High",
                    Detail =
                        $"'{declaration.Namespace}.{declaration.TypeName}' is a TypeOption. Its module-initializer " +
                        "registration is emitted by whichever compilation contains it, so moving it changes which " +
                        "assembly registers it. Verify the owning TypeCollection is still populated AT RUNTIME — a " +
                        "clean build does not prove this.",
                });
            }
        }

        return findings;
    }

    /// <summary>
    /// Scans the whole solution for references to the source assembly by name, which a move invalidates.
    /// </summary>
    /// <param name="solution">The solution to scan.</param>
    /// <param name="sourceAssembly">The assembly the documents are leaving.</param>
    /// <param name="command">The command whose generated-file policy applies.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The findings.</returns>
    public static async Task<IReadOnlyList<BreakFinding>> ScanAssemblyNameReferences(
        Solution solution,
        string sourceAssembly,
        RoslynCommandBase command,
        CancellationToken cancellationToken = default)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        var findings = new List<BreakFinding>();
        if (string.IsNullOrEmpty(sourceAssembly)) return findings;

        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (command.IsGeneratedDocument(document)) continue;

                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                if (root is null) continue;

                findings.AddRange(ScanLiterals(root, document, sourceAssembly));
                findings.AddRange(ScanAttributes(root, document, sourceAssembly));
            }
        }

        return findings;
    }

    private static IEnumerable<BreakFinding> ScanLiterals(SyntaxNode root, Document document, string sourceAssembly)
    {
        foreach (var literal in root.DescendantNodes().OfType<LiteralExpressionSyntax>())
        {
            var text = literal.Token.ValueText;
            if (string.IsNullOrEmpty(text)) continue;
            if (!text.Contains(sourceAssembly, StringComparison.Ordinal)) continue;

            yield return new BreakFinding
            {
                Kind = "AssemblyQualifiedTypeString",
                FilePath = document.FilePath ?? string.Empty,
                Severity = "High",
                Detail = $"String literal names the source assembly and will no longer resolve: \"{Truncate(text)}\"",
            };
        }
    }

    private static IEnumerable<BreakFinding> ScanAttributes(SyntaxNode root, Document document, string sourceAssembly)
    {
        foreach (var attribute in root.DescendantNodes().OfType<AttributeSyntax>())
        {
            var name = attribute.Name.ToString();
            if (!name.Contains("InternalsVisibleTo", StringComparison.Ordinal)) continue;

            var argument = attribute.ArgumentList?.Arguments.ToString() ?? string.Empty;
            if (!argument.Contains(sourceAssembly, StringComparison.Ordinal)) continue;

            yield return new BreakFinding
            {
                Kind = "InternalsVisibleTo",
                FilePath = document.FilePath ?? string.Empty,
                Severity = "Medium",
                Detail = $"[InternalsVisibleTo] names the source assembly; internals of the moved types are no longer covered: {Truncate(argument)}",
            };
        }
    }

    private static string Truncate(string value) =>
        value.Length <= 120 ? value : string.Concat(value.AsSpan(0, 117), "...");
}
