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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Fdw.Conventions;

namespace Fdw.Roslyn.Commands.Generation.Translators;

/// <summary>
/// Translator for GenerateTestsCommand.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "GenerateTests")]
public sealed class GenerateTestsTranslator : RoslynCommandTranslatorBase<GenerateTestsCommand, MutationResult>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateTestsTranslator"/> class.
    /// </summary>
    public GenerateTestsTranslator()
        : base("GenerateTests", "Generates unit tests for a class")
    {
    }

    /// <inheritdoc/>
#pragma warning disable MA0051 // Linear code generation: analyze class, build test file via StringBuilder
    [ConventionOverride(MaxMethodLines = 140)]
    public override async Task<IGenericResult<MutationResult>> Translate(
        GenerateTestsCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var documentId = solution.GetDocumentIdsWithFilePath(command.FilePath).FirstOrDefault();
        if (documentId is null)
        {
            GenerateTestsTranslatorLog.DocumentNotFound(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("DocumentNotFound"),
                ResultDetails.Create().With("FilePath", command.FilePath));
        }

        var document = solution.GetDocument(documentId);
        if (document is null)
        {
            GenerateTestsTranslatorLog.FailedToLoadDocument(Logger, command.FilePath);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToLoadDocument"));
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

        if (semanticModel is null || syntaxRoot is null)
        {
            GenerateTestsTranslatorLog.FailedToAnalyzeDocument(Logger, command.FilePath);
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
            GenerateTestsTranslatorLog.NoTypeDeclarationFoundAtPosition(Logger, command.FilePath, command.Line, command.Column);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("NoTypeDeclarationFoundAtPosition"));
        }

        var symbol = semanticModel.GetDeclaredSymbol(typeDecl, cancellationToken);
        if (symbol is not INamedTypeSymbol typeSymbol)
        {
            GenerateTestsTranslatorLog.FailedToGetTypeSymbol(Logger, command.FilePath, command.Line, command.Column);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToGetTypeSymbol"));
        }

        var typeName = typeSymbol.Name;
        var ns = typeSymbol.ContainingNamespace.ToDisplayString();

        // Get public methods to test
        var publicMethods = typeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.DeclaredAccessibility == Accessibility.Public &&
                        m.MethodKind == MethodKind.Ordinary &&
                        !m.IsStatic)
            .ToList();

        if (publicMethods.Count == 0)
        {
            GenerateTestsTranslatorLog.NoPublicMethodsFoundToGenerateTests(Logger, typeName);
            return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("NoPublicMethodsFoundToGenerateTests"));
        }

        GenerateTestsTranslatorLog.Generating(Logger, command.FilePath, command.Line, command.Column, command.TestFramework);

        var sb = new StringBuilder();
        var (testAttribute, factAttribute) = GetFrameworkAttributes(command.TestFramework);

        // Usings
        sb.AppendLine("using System;");
        sb.AppendLine($"using {ns};");

        switch (command.TestFramework.ToLowerInvariant())
        {
            case "xunit":
                sb.AppendLine("using Xunit;");
                break;
            case "nunit":
                sb.AppendLine("using NUnit.Framework;");
                break;
            case "mstest":
                sb.AppendLine("using Microsoft.VisualStudio.TestTools.UnitTesting;");
                break;
        }

        sb.AppendLine();

        // Namespace
        sb.AppendLine($"namespace {ns}.Tests;");
        sb.AppendLine();

        // Test class
        if (string.Equals(command.TestFramework, "mstest", StringComparison.OrdinalIgnoreCase))
            sb.AppendLine("[TestClass]");

        sb.AppendLine($"public class {typeName}Tests");
        sb.AppendLine("{");

        // Generate test methods
        foreach (var method in publicMethods)
        {
            var testMethodName = $"{method.Name}ReturnsExpectedResult";

            sb.AppendLine($"    [{factAttribute}]");
            sb.AppendLine($"    public void {testMethodName}()");
            sb.AppendLine("    {");
            sb.AppendLine("        // Arrange");
            sb.AppendLine($"        // var sut = new {typeName}();");
            sb.AppendLine();
            sb.AppendLine("        // Act");
            sb.AppendLine($"        // var result = sut.{method.Name}();");
            sb.AppendLine();
            sb.AppendLine("        // Assert");
            sb.AppendLine("        throw new NotImplementedException();");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        var generatedCode = sb.ToString();
        var compilationUnit = SyntaxFactory.ParseCompilationUnit(generatedCode);

        // Determine target project
        Project? targetProject = null;
        if (!string.IsNullOrEmpty(command.TestProjectName))
        {
            targetProject = solution.Projects.FirstOrDefault(p => string.Equals(p.Name, command.TestProjectName, StringComparison.Ordinal));
            if (targetProject is null)
            {
                GenerateTestsTranslatorLog.TestProjectNotFound(Logger, command.TestProjectName);
                return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("TestProjectNotFound"),
                ResultDetails.Create().With("TestProjectName", command.TestProjectName));
            }
        }
        else
        {
            // Try to find a test project
            targetProject = solution.Projects.FirstOrDefault(p => p.Name.Contains("Test", StringComparison.OrdinalIgnoreCase))
                ?? solution.Projects.FirstOrDefault();

            if (targetProject is null)
            {
                GenerateTestsTranslatorLog.NoProjectsFoundInSolution(Logger);
                return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("NoProjectsFoundInSolution"));
            }
        }

        var fileName = $"{typeName}Tests.cs";

        // Check if document already exists
        var existingDocId = targetProject.Documents.FirstOrDefault(d => string.Equals(d.Name, fileName, StringComparison.Ordinal))?.Id;
        Document newDocument;

        if (existingDocId is not null)
        {
            // Update existing document
            var existingDoc = targetProject.GetDocument(existingDocId);
            if (existingDoc is null)
            {
                GenerateTestsTranslatorLog.FailedToLoadExistingDocument(Logger, fileName);
                return GenericResult<MutationResult>.Failure(
                RoslynResultCodes.ByName("FailedToLoadExistingDocument"));
            }

            newDocument = existingDoc.WithSyntaxRoot(compilationUnit);
        }
        else
        {
            // Create new document
            newDocument = targetProject.AddDocument(fileName, SourceText.From(generatedCode), null, fileName);
        }

        var newSolution = newDocument.Project.Solution;

        var fileChanges = new List<FileChange>
        {
            new FileChange(fileName, existingDocId is not null ? FileChangeTypes.Modified : FileChangeTypes.Added, targetProject.Name)
            {
                TextChangeCount = publicMethods.Count
            }
        };

        GenerateTestsTranslatorLog.Generated(Logger, typeName, publicMethods.Count);

        return GenericResult<MutationResult>.Success(
            new MutationResult(
                $"Generated {publicMethods.Count} unit tests for '{typeName}'",
                newSolution,
                fileChanges));
    }
#pragma warning restore MA0051

    private static (string testClass, string fact) GetFrameworkAttributes(string framework)
    {
        return framework.ToLowerInvariant() switch
        {
            "nunit" => ("TestFixture", "Test"),
            "mstest" => ("TestClass", "TestMethod"),
            _ => ("", "Fact")
        };
    }
}
