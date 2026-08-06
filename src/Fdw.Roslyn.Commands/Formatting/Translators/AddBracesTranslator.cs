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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Roslyn.Commands.Formatting.Translators;

/// <summary>
/// Translator for AddBracesCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "AddBraces")]
public sealed class AddBracesTranslator : RoslynCommandTranslatorBase<AddBracesCommand, MutationResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddBracesTranslator"/> class.
    /// </summary>
    public AddBracesTranslator()
        : base("AddBraces", "Adds braces to single-line statements")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<MutationResult>> Translate(
        AddBracesCommand command,
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

        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (syntaxRoot is null)
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToGetSyntaxRoot"));

        var rewriter = new AddBracesRewriter();
        var newRoot = rewriter.Visit(syntaxRoot);

        var newDocument = document.WithSyntaxRoot(newRoot);
        var newSolution = newDocument.Project.Solution;

        var fileChanges = new List<FileChange>();
        if (rewriter.ChangeCount > 0)
        {
            fileChanges.Add(new FileChange(command.FilePath, FileChangeTypes.Modified, document.Project.Name)
            {
                TextChangeCount = rewriter.ChangeCount
            });
        }

        return GenericResult<MutationResult>.Success(
            new MutationResult(
                $"Added braces to {rewriter.ChangeCount} statements",
                newSolution,
                fileChanges));
    }

    private sealed class AddBracesRewriter : CSharpSyntaxRewriter
    {
        public int ChangeCount { get; private set; }

        public override SyntaxNode? VisitIfStatement(IfStatementSyntax node)
        {
            var newNode = base.VisitIfStatement(node) as IfStatementSyntax;
            if (newNode is null)
                return node;

            if (newNode.Statement is not BlockSyntax)
            {
                var block = SyntaxFactory.Block(newNode.Statement);
                newNode = newNode.WithStatement(block);
                ChangeCount++;
            }

            return newNode;
        }

        public override SyntaxNode? VisitElseClause(ElseClauseSyntax node)
        {
            var newNode = base.VisitElseClause(node) as ElseClauseSyntax;
            if (newNode is null)
                return node;

            if (newNode.Statement is not BlockSyntax && newNode.Statement is not IfStatementSyntax)
            {
                var block = SyntaxFactory.Block(newNode.Statement);
                newNode = newNode.WithStatement(block);
                ChangeCount++;
            }

            return newNode;
        }

        public override SyntaxNode? VisitForStatement(ForStatementSyntax node)
        {
            var newNode = base.VisitForStatement(node) as ForStatementSyntax;
            if (newNode is null)
                return node;

            if (newNode.Statement is not BlockSyntax)
            {
                var block = SyntaxFactory.Block(newNode.Statement);
                newNode = newNode.WithStatement(block);
                ChangeCount++;
            }

            return newNode;
        }

        public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
        {
            var newNode = base.VisitForEachStatement(node) as ForEachStatementSyntax;
            if (newNode is null)
                return node;

            if (newNode.Statement is not BlockSyntax)
            {
                var block = SyntaxFactory.Block(newNode.Statement);
                newNode = newNode.WithStatement(block);
                ChangeCount++;
            }

            return newNode;
        }

        public override SyntaxNode? VisitWhileStatement(WhileStatementSyntax node)
        {
            var newNode = base.VisitWhileStatement(node) as WhileStatementSyntax;
            if (newNode is null)
                return node;

            if (newNode.Statement is not BlockSyntax)
            {
                var block = SyntaxFactory.Block(newNode.Statement);
                newNode = newNode.WithStatement(block);
                ChangeCount++;
            }

            return newNode;
        }

        public override SyntaxNode? VisitUsingStatement(UsingStatementSyntax node)
        {
            var newNode = base.VisitUsingStatement(node) as UsingStatementSyntax;
            if (newNode is null)
                return node;

            if (newNode.Statement is not BlockSyntax && newNode.Statement is not UsingStatementSyntax)
            {
                var block = SyntaxFactory.Block(newNode.Statement);
                newNode = newNode.WithStatement(block);
                ChangeCount++;
            }

            return newNode;
        }

        public override SyntaxNode? VisitLockStatement(LockStatementSyntax node)
        {
            var newNode = base.VisitLockStatement(node) as LockStatementSyntax;
            if (newNode is null)
                return node;

            if (newNode.Statement is not BlockSyntax)
            {
                var block = SyntaxFactory.Block(newNode.Statement);
                newNode = newNode.WithStatement(block);
                ChangeCount++;
            }

            return newNode;
        }
    }
}
