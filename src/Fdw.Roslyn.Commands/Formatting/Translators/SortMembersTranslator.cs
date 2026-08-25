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
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Formatting.Translators;

/// <summary>
/// Translator for SortMembersCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "SortMembers")]
public sealed class SortMembersTranslator : RoslynCommandTranslatorBase<SortMembersCommand, MutationResult<SortedMembersData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SortMembersTranslator"/> class.
    /// </summary>
    public SortMembersTranslator()
        : base("SortMembers", "Sorts members within a type")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: get members, sort by kind/accessibility/name, rebuild type
    public override async Task<IGenericResult<MutationResult<SortedMembersData>>> Translate(
        SortMembersCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        SortMembersTranslatorLog.Sorting(Logger, command.FilePath, command.Line);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            SortMembersTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<MutationResult<SortedMembersData>>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            SortMembersTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<MutationResult<SortedMembersData>>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (syntaxRoot is null)
        {
            SortMembersTranslatorLog.FailedToAnalyzeDocument(Logger, command.FilePath);
            return GenericResult<MutationResult<SortedMembersData>>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));
        }

        // Collect the type declarations to sort. If a position is supplied (Line >= 1),
        // sort just the containing type. If Line is 0 (omitted), sort every type in the
        // document — which matches the command's description ("Sort members within each
        // type").
        var typeDecls = new List<TypeDeclarationSyntax>();
        if (command.Line >= 1)
        {
            var position = text.Lines.GetPosition(new LinePosition(command.Line - 1, Math.Max(0, command.Column - 1)));
            var token = syntaxRoot.FindToken(position);
            var typeDecl = token.Parent?.AncestorsAndSelf()
                .OfType<TypeDeclarationSyntax>()
                .FirstOrDefault();

            if (typeDecl is null)
            {
                SortMembersTranslatorLog.NoTypeDeclarationFoundAtPosition(Logger, command.FilePath, command.Line);
                return GenericResult<MutationResult<SortedMembersData>>.Failure(
                    RoslynResultCodes.ByName("NoTypeDeclarationFoundAtPosition"));
            }

            typeDecls.Add(typeDecl);
        }
        else
        {
            typeDecls.AddRange(syntaxRoot.DescendantNodes().OfType<TypeDeclarationSyntax>());
        }

        // Process all selected type declarations in a single pass, building the new root
        // by replacing each in turn. Because ReplaceNode rebuilds the tree, look up the
        // current root each iteration.
        var newRoot = syntaxRoot;
        var totalMembers = 0;
        var totalChangedTypes = 0;
        var firstTypeName = string.Empty;
        var memberOrder = new List<FormattedMemberInfo>();

        foreach (var originalTypeDecl in typeDecls)
        {
            // The node we want to replace may have been rebuilt by a prior replacement;
            // map the original to its current incarnation via SpanStart.
            var typeDecl = newRoot.DescendantNodes()
                .OfType<TypeDeclarationSyntax>()
                .FirstOrDefault(t => string.Equals(t.Identifier.Text, originalTypeDecl.Identifier.Text, StringComparison.Ordinal)
                                  && t.SpanStart == originalTypeDecl.SpanStart);
            if (typeDecl is null) continue;

            if (firstTypeName.Length == 0)
                firstTypeName = typeDecl.Identifier.Text;

            var members = typeDecl.Members
                .Select(m => new
                {
                    Member = m,
                    Kind = GetMemberKind(m),
                    Name = GetMemberName(m),
                    Accessibility = GetAccessibility(m)
                })
                .ToList();

            var sortedMembers = members
                .OrderBy(m => m.Kind)
                .ThenByDescending(m => m.Accessibility)
                .ThenBy(m => m.Name, StringComparer.Ordinal)
                .ToList();

            totalMembers += members.Count;

            var isSorted = members.Select(m => m.Member).SequenceEqual(sortedMembers.Select(m => m.Member));
            if (!isSorted)
            {
                var newTypeDecl = typeDecl.WithMembers(SyntaxFactory.List(sortedMembers.Select(m => m.Member)));
                newRoot = newRoot.ReplaceNode(typeDecl, newTypeDecl);
                totalChangedTypes++;
            }

            // Capture the first type's sorted layout in the response data for backward
            // compatibility with single-type callers.
            if (memberOrder.Count == 0)
            {
                memberOrder.AddRange(sortedMembers.Select(m => new FormattedMemberInfo
                {
                    Name = m.Name,
                    Kind = GetKindName(m.Kind),
                    Accessibility = m.Accessibility.ToString()
                }));
            }
        }

        var newSolution = solution;
        var fileChanges = new List<FileChange>();
        if (totalChangedTypes > 0)
        {
            var newDocument = document.WithSyntaxRoot(newRoot);
            newSolution = newDocument.Project.Solution;
            fileChanges.Add(new FileChange(command.FilePath, FileChangeTypes.Modified, document.Project.Name)
            {
                TextChangeCount = totalMembers
            });
        }

        var data = new SortedMembersData
        {
            TypeName = firstTypeName,
            MemberCount = totalMembers,
            SortedMembers = memberOrder
        };

        SortMembersTranslatorLog.Sorted(Logger, command.FilePath, totalMembers, totalChangedTypes);

        return GenericResult<MutationResult<SortedMembersData>>.Success(
            new MutationResult<SortedMembersData>(
                $"Sorted {totalMembers} members across {totalChangedTypes} changed type(s)",
                newSolution,
                fileChanges,
                data));
    }
#pragma warning restore MA0051

    private static int GetMemberKind(MemberDeclarationSyntax member)
    {
        return member switch
        {
            FieldDeclarationSyntax => 0,
            ConstructorDeclarationSyntax => 1,
            PropertyDeclarationSyntax => 2,
            IndexerDeclarationSyntax => 3,
            MethodDeclarationSyntax => 4,
            EventDeclarationSyntax => 5,
            EventFieldDeclarationSyntax => 5,
            TypeDeclarationSyntax => 6,
            _ => 99
        };
    }

    private static string GetKindName(int kind)
    {
        return kind switch
        {
            0 => "Field",
            1 => "Constructor",
            2 => "Property",
            3 => "Indexer",
            4 => "Method",
            5 => "Event",
            6 => "NestedType",
            _ => "Other"
        };
    }

    private static string GetMemberName(MemberDeclarationSyntax member)
    {
        return member switch
        {
            FieldDeclarationSyntax f => f.Declaration.Variables.FirstOrDefault()?.Identifier.Text ?? string.Empty,
            ConstructorDeclarationSyntax c => c.Identifier.Text,
            PropertyDeclarationSyntax p => p.Identifier.Text,
            IndexerDeclarationSyntax => "this",
            MethodDeclarationSyntax m => m.Identifier.Text,
            EventDeclarationSyntax e => e.Identifier.Text,
            TypeDeclarationSyntax t => t.Identifier.Text,
            _ => string.Empty
        };
    }

    private static Accessibility GetAccessibility(MemberDeclarationSyntax member)
    {
        var modifiers = member.Modifiers;

        if (modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
            return Accessibility.Public;
        if (modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword)) &&
            modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword)))
            return Accessibility.ProtectedOrInternal;
        if (modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword)))
            return Accessibility.Protected;
        if (modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword)))
            return Accessibility.Internal;
        // Default for class members and explicit private
        return Accessibility.Private;
    }
}
