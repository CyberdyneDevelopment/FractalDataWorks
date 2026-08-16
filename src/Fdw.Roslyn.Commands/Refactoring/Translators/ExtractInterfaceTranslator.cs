using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Analysis.Helpers;
using Fdw.Roslyn.Commands.Logging;
using Fdw.Roslyn.Commands.Refactoring.Commands;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Fdw.Roslyn.Commands.Refactoring.Translators;

/// <summary>
/// Translator for ExtractInterfaceCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "ExtractInterface")]
public sealed class ExtractInterfaceTranslator : RoslynCommandTranslatorBase<ExtractInterfaceCommand, MutationResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractInterfaceTranslator"/> class.
    /// </summary>
    public ExtractInterfaceTranslator()
        : base("ExtractInterface", "Extracts an interface from a class")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear Roslyn flow: resolve type, build interface members, add to document
    public override async Task<IGenericResult<MutationResult>> Translate(
        ExtractInterfaceCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        ExtractInterfaceTranslatorLog.Extracting(Logger, command.FilePath, command.Line, command.Column);

        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            ExtractInterfaceTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            ExtractInterfaceTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
        {
            ExtractInterfaceTranslatorLog.FailedToAnalyzeDocument(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToAnalyzeDocument"));
        }

        var position = text.Lines.GetPosition(new LinePosition(command.Line - 1, command.Column - 1));
        var token = syntaxRoot.FindToken(position);

        // Find the type declaration
        var typeDecl = token.Parent?.AncestorsAndSelf()
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault();

        if (typeDecl is null)
        {
            ExtractInterfaceTranslatorLog.NoTypeDeclarationFoundAtPosition(Logger, command.FilePath, command.Line, command.Column);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("NoTypeDeclarationFoundAtPosition"));
        }

        var symbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);
        if (symbol is not INamedTypeSymbol typeSymbol)
        {
            ExtractInterfaceTranslatorLog.FailedToGetTypeSymbol(Logger, command.FilePath, command.Line, command.Column);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToGetTypeSymbol"));
        }

        var typeName = typeSymbol.Name;
        var interfaceName = command.InterfaceName ?? $"I{typeName}";

        // Build interface members from public members
        var interfaceMembers = new List<MemberDeclarationSyntax>();

        foreach (var member in typeSymbol.GetMembers())
        {
            if (member.DeclaredAccessibility != Accessibility.Public || member.IsStatic)
                continue;

            if (member is IMethodSymbol method && method.MethodKind == MethodKind.Ordinary)
            {
                var methodDecl = BuildInterfaceMethod(method);
                if (methodDecl is not null)
                    interfaceMembers.Add(methodDecl);
            }
            else if (member is IPropertySymbol property)
            {
                var propertyDecl = BuildInterfaceProperty(property);
                if (propertyDecl is not null)
                    interfaceMembers.Add(propertyDecl);
            }
        }

        // Create interface declaration
        var interfaceDecl = SyntaxFactory.InterfaceDeclaration(interfaceName)
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithMembers(SyntaxFactory.List(interfaceMembers))
            .WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        // Add interface to document (before the class)
        var newRoot = syntaxRoot.InsertNodesBefore(typeDecl, new[] { interfaceDecl });

        // Add interface to class's base list
        var baseType = SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(interfaceName));
        var newTypeDecl = typeDecl.BaseList is null
            ? typeDecl.WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(baseType)))
            : typeDecl.WithBaseList(typeDecl.BaseList.AddTypes(baseType));

        newRoot = newRoot.ReplaceNode(newRoot.DescendantNodes().OfType<TypeDeclarationSyntax>().First(t => string.Equals(t.Identifier.Text, typeName, StringComparison.Ordinal)), newTypeDecl);

        var newDocument = document.WithSyntaxRoot(newRoot);
        var newSolution = newDocument.Project.Solution;

        var fileChanges = new List<FileChange>
        {
            new FileChange(command.FilePath, FileChangeTypes.Modified, document.Project.Name)
            {
                TextChangeCount = interfaceMembers.Count + 1
            }
        };

        // Why: for an Added symbol change, "old" is the source symbol it was extracted FROM and
        // "new" is the created symbol, so the guide reads "extracted from X → created Y".
        var oldFqn = SymbolFqn.Of(typeSymbol);
        var newFqn = SymbolFqn.OfRenamed(typeSymbol, interfaceName);
        var symbolChanges = new List<SymbolChange>
        {
            new SymbolChange(
                oldFqn, newFqn, SymbolChangeTypes.Added.Name, "NamedType",
                document.FilePath, document.FilePath,
                document.Project.AssemblyName, document.Project.AssemblyName,
                NamespaceLayout.RelativePosition(document.Project, document.FilePath))
        };

        ExtractInterfaceTranslatorLog.Extracted(Logger, interfaceName, typeName, interfaceMembers.Count);

        return GenericResult<MutationResult>.Success(
            new MutationResult(
                $"Extracted interface '{interfaceName}' with {interfaceMembers.Count} members from '{typeName}'",
                newSolution,
                fileChanges,
                symbolChanges,
                Array.Empty<PathChange>()));
    }
#pragma warning restore MA0051

    private static MethodDeclarationSyntax? BuildInterfaceMethod(IMethodSymbol method)
    {
        var returnType = SyntaxFactory.ParseTypeName(method.ReturnType.ToDisplayString());
        var parameters = SyntaxFactory.ParameterList(
            SyntaxFactory.SeparatedList(
                method.Parameters.Select(p =>
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(p.Name))
                        .WithType(SyntaxFactory.ParseTypeName(p.Type.ToDisplayString())))));

        return SyntaxFactory.MethodDeclaration(returnType, method.Name)
            .WithParameterList(parameters)
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private static PropertyDeclarationSyntax? BuildInterfaceProperty(IPropertySymbol property)
    {
        var propertyType = SyntaxFactory.ParseTypeName(property.Type.ToDisplayString());
        var accessors = new List<AccessorDeclarationSyntax>();

        if (property.GetMethod is not null)
        {
            accessors.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
        }

        if (property.SetMethod is not null)
        {
            accessors.Add(SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
        }

        return SyntaxFactory.PropertyDeclaration(propertyType, property.Name)
            .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(accessors)));
    }
}
