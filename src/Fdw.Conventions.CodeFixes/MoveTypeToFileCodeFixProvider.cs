using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Conventions.CodeFixes;

/// <summary>
/// Code fix provider that moves a type declaration to its own file.
/// Fixes FDW005 (file name must match type name) and MA0048 (Meziantou equivalent).
/// When a target file already exists, the type is merged into it.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MoveTypeToFileCodeFixProvider)), Shared]
public class MoveTypeToFileCodeFixProvider : CodeFixProvider
{
    private const string Title = "Move type to file '{0}.cs'";

    /// <summary>
    /// Gets the diagnostic IDs that this provider can fix.
    /// </summary>
    public sealed override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create("FDW005", "MA0048");

    /// <summary>
    /// Gets the fix all provider for this code fix provider.
    /// </summary>
    /// <returns>The fix all provider.</returns>
    public sealed override FixAllProvider GetFixAllProvider()
        => MoveTypeToFileFixAllProvider.Instance;

    /// <summary>
    /// Registers code fixes for the specified context.
    /// </summary>
    /// <param name="context">The code fix context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var typeDeclaration = FindTypeDeclaration(root, diagnosticSpan.Start);
        if (typeDeclaration == null)
            return;

        var typeName = typeDeclaration.Identifier.Text;
        var title = string.Format(System.Globalization.CultureInfo.InvariantCulture, Title, typeName);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedSolution: c => MoveTypesToFiles(
                    context.Document, [typeDeclaration], c),
                equivalenceKey: title),
            diagnostic);
    }

    /// <summary>
    /// Moves one or more types from a source document into their own files.
    /// Processes all types in a single pass to avoid span invalidation issues.
    /// </summary>
    internal static async Task<Solution> MoveTypesToFiles(
        Document document,
        IReadOnlyList<BaseTypeDeclarationSyntax> typeDeclarations,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document.Project.Solution;

        var compilationUnit = (CompilationUnitSyntax)root;
        var folders = document.Folders;

        // Phase 1: Remove ALL flagged types from the source document in one operation
        var newSourceRoot = root.RemoveNodes(
            typeDeclarations,
            SyntaxRemoveOptions.KeepLeadingTrivia | SyntaxRemoveOptions.KeepTrailingTrivia);

        var solution = document.Project.Solution;
        solution = solution.WithDocumentSyntaxRoot(document.Id, newSourceRoot ?? root);

        // Phase 2: Group types by target file name — types with the same name go to the same file
        var groupsByFile = new Dictionary<string, List<BaseTypeDeclarationSyntax>>(StringComparer.Ordinal);
        foreach (var typeDecl in typeDeclarations)
        {
            var targetFileName = typeDecl.Identifier.Text + ".cs";
            if (!groupsByFile.TryGetValue(targetFileName, out var list))
            {
                list = [];
                groupsByFile[targetFileName] = list;
            }

            list.Add(typeDecl);
        }

        // Phase 3: For each target file, create or merge
        foreach (var group in groupsByFile)
        {
            var targetFileName = group.Key;
            var types = group.Value;

            var project = solution.GetProject(document.Project.Id);
            if (project == null)
                break;

            var existingDocument = FindExistingDocument(project, targetFileName, folders);
            if (existingDocument != null)
            {
                solution = await MergeTypesIntoExistingFile(
                    solution, existingDocument, compilationUnit, types, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                solution = CreateNewFileWithTypes(
                    solution, document.Project.Id, targetFileName, folders, compilationUnit, types);
            }
        }

        return solution;
    }

    internal static BaseTypeDeclarationSyntax? FindTypeDeclaration(SyntaxNode root, int position)
    {
        return root.FindToken(position)
            .Parent?
            .AncestorsAndSelf()
            .OfType<BaseTypeDeclarationSyntax>()
            .FirstOrDefault();
    }

    private static Document? FindExistingDocument(
        Project project, string fileName, IReadOnlyList<string> folders)
    {
        foreach (var doc in project.Documents)
        {
            if (!string.Equals(
                    System.IO.Path.GetFileName(doc.Name),
                    fileName,
                    StringComparison.OrdinalIgnoreCase))
                continue;

            if (doc.Folders.Count != folders.Count)
                continue;

            var match = true;
            for (var i = 0; i < folders.Count; i++)
            {
                if (!string.Equals(doc.Folders[i], folders[i], StringComparison.Ordinal))
                {
                    match = false;
                    break;
                }
            }

            if (match)
                return doc;
        }

        return null;
    }

    private static async Task<Solution> MergeTypesIntoExistingFile(
        Solution solution,
        Document existingDocument,
        CompilationUnitSyntax sourceCompilation,
        IReadOnlyList<BaseTypeDeclarationSyntax> typeDeclarations,
        CancellationToken cancellationToken)
    {
        // Re-resolve the document from the current solution (it may have been updated by a prior merge)
        existingDocument = solution.GetDocument(existingDocument.Id) ?? existingDocument;

        var existingRoot = await existingDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (existingRoot is not CompilationUnitSyntax existingCompilation)
            return solution;

        // Merge using directives
        var mergedUsings = MergeUsings(existingCompilation.Usings, sourceCompilation.Usings);

        var currentCompilation = existingCompilation;

        foreach (var typeDeclaration in typeDeclarations)
        {
            var typeAsMember = (MemberDeclarationSyntax)typeDeclaration
                .WithLeadingTrivia(GetTypeLeadingTrivia(typeDeclaration));

            var sourceNamespace = typeDeclaration.Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
            var sourceNamespaceName = sourceNamespace?.Name.ToString();

            if (sourceNamespaceName != null)
            {
                var matchingNamespace = currentCompilation.Members
                    .OfType<BaseNamespaceDeclarationSyntax>()
                    .FirstOrDefault(ns => string.Equals(
                        ns.Name.ToString(), sourceNamespaceName, StringComparison.Ordinal));

                if (matchingNamespace != null)
                {
                    var updatedNamespace = matchingNamespace.AddMembers(typeAsMember);
                    currentCompilation = currentCompilation.ReplaceNode(matchingNamespace, updatedNamespace);
                }
                else
                {
                    // Different namespace — add block namespace (file-scoped can only appear once)
                    var newNamespace = SyntaxFactory.NamespaceDeclaration(
                            SyntaxFactory.ParseName(sourceNamespaceName))
                        .WithMembers(SyntaxFactory.SingletonList(typeAsMember));

                    currentCompilation = currentCompilation.AddMembers(newNamespace);
                }
            }
            else
            {
                currentCompilation = currentCompilation.AddMembers(typeAsMember);
            }
        }

        currentCompilation = currentCompilation.WithUsings(mergedUsings);
        return solution.WithDocumentSyntaxRoot(existingDocument.Id, currentCompilation);
    }

    private static Solution CreateNewFileWithTypes(
        Solution solution,
        ProjectId projectId,
        string targetFileName,
        IReadOnlyList<string> folders,
        CompilationUnitSyntax sourceCompilation,
        IReadOnlyList<BaseTypeDeclarationSyntax> typeDeclarations)
    {
        var usings = sourceCompilation.Usings;
        var externs = sourceCompilation.Externs;

        // All types in this group share the same target file name.
        // Build members grouped by namespace.
        var membersByNamespace = new Dictionary<string, List<MemberDeclarationSyntax>>(StringComparer.Ordinal);
        var noNamespaceMembers = new List<MemberDeclarationSyntax>();
        BaseNamespaceDeclarationSyntax? firstNamespaceDecl = null;

        foreach (var typeDeclaration in typeDeclarations)
        {
            var typeAsMember = (MemberDeclarationSyntax)typeDeclaration
                .WithLeadingTrivia(GetTypeLeadingTrivia(typeDeclaration));

            var containingNamespace = typeDeclaration.Ancestors()
                .OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();

            if (containingNamespace != null)
            {
                var nsName = containingNamespace.Name.ToString();
                if (!membersByNamespace.TryGetValue(nsName, out var list))
                {
                    list = [];
                    membersByNamespace[nsName] = list;
                }

                list.Add(typeAsMember);
                firstNamespaceDecl ??= containingNamespace;
            }
            else
            {
                noNamespaceMembers.Add(typeAsMember);
            }
        }

        // Build the compilation unit
        var allMembers = new List<MemberDeclarationSyntax>();

        // Add namespaced members
        foreach (var kvp in membersByNamespace)
        {
            var nsName = kvp.Key;
            var members = SyntaxFactory.List<MemberDeclarationSyntax>(kvp.Value);

            // Use file-scoped namespace if the source used it and there's only one namespace
            if (firstNamespaceDecl is FileScopedNamespaceDeclarationSyntax fileScopedNs
                && membersByNamespace.Count == 1
                && noNamespaceMembers.Count == 0)
            {
                var newNamespace = fileScopedNs
                    .WithMembers(members)
                    .WithLeadingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.LineFeed))
                    .WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.LineFeed));

                allMembers.Add(newNamespace);
            }
            else
            {
                var blockNs = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(nsName))
                    .WithMembers(members);

                allMembers.Add(blockNs);
            }
        }

        // Add non-namespaced members
        allMembers.AddRange(noNamespaceMembers);

        var newRoot = SyntaxFactory.CompilationUnit()
            .WithExterns(externs)
            .WithUsings(usings)
            .WithMembers(SyntaxFactory.List(allMembers))
            .NormalizeWhitespace()
            .WithTrailingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.LineFeed));

        var project = solution.GetProject(projectId);
        if (project == null)
            return solution;

        var newDocument = project.AddDocument(targetFileName, newRoot, folders);
        return newDocument.Project.Solution;
    }

    private static SyntaxList<UsingDirectiveSyntax> MergeUsings(
        SyntaxList<UsingDirectiveSyntax> existing,
        SyntaxList<UsingDirectiveSyntax> incoming)
    {
        var existingNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var u in existing)
        {
            existingNames.Add(u.Name?.ToString() ?? u.ToString());
        }

        var merged = new List<UsingDirectiveSyntax>(existing);
        foreach (var u in incoming)
        {
            var name = u.Name?.ToString() ?? u.ToString();
            if (!existingNames.Contains(name))
            {
                merged.Add(u);
                existingNames.Add(name);
            }
        }

        return SyntaxFactory.List(merged);
    }

    internal static SyntaxTriviaList GetTypeLeadingTrivia(BaseTypeDeclarationSyntax typeDeclaration)
    {
        var leadingTrivia = typeDeclaration.GetLeadingTrivia();
        var significantTrivia = new List<SyntaxTrivia>();

        foreach (var trivia in leadingTrivia)
        {
            if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)
                || trivia.IsKind(SyntaxKind.PragmaWarningDirectiveTrivia)
                || trivia.IsKind(SyntaxKind.RegionDirectiveTrivia)
                || trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia)
                || trivia.IsKind(SyntaxKind.SingleLineCommentTrivia)
                || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
                || trivia.IsKind(SyntaxKind.WhitespaceTrivia)
                || trivia.IsKind(SyntaxKind.EndOfLineTrivia))
            {
                significantTrivia.Add(trivia);
            }
        }

        return SyntaxFactory.TriviaList(significantTrivia);
    }
}
