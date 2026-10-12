using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Conventions.Analyzers;

/// <summary>
/// Analyzer that enforces file names must match the type name declared within.
/// Replaces MA0048 with support for generic arity variants (e.g., Foo&lt;T&gt; and Foo&lt;T,U&gt; in Foo.cs).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FileNameMustMatchTypeNameAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for file name must match type name violation.
    /// </summary>
    public const string DiagnosticId = "FDW005";

    private const string Title = "File name must match type name";
    private const string MessageFormat = "Type '{0}' should be declared in a file named '{0}.cs' (current file: '{1}'). Run 'dotnet fdw-split {1}' to fix.";
    private const string Description = "Each file should contain types whose base name matches the file name. Generic arity variants are allowed in the same file. Use 'dotnet fdw-split' to batch-fix.";
    private const string Category = "Naming";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
    }

    private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
    {
        var filePath = context.Tree.FilePath;
        if (string.IsNullOrEmpty(filePath))
            return;

        // Skip generated files
        var fileName = Path.GetFileName(filePath);
        if (IsGeneratedFile(fileName))
            return;

        // Extract primary filename: take part before first '.' then compare
        // e.g., "Foo.Part1.cs" -> "Foo", "Foo.razor.cs" -> "Foo", "Foo.cs" -> "Foo"
        var primaryName = GetPrimaryFileName(fileName);
        if (string.IsNullOrEmpty(primaryName))
            return;

        var root = context.Tree.GetRoot(context.CancellationToken);

        // Find all top-level type declarations (skip nested types)
        // Uses BaseTypeDeclarationSyntax to catch class, struct, record, interface, AND enum
        var topLevelTypes = GetTopLevelTypeDeclarations(root);

        // Report diagnostics on types whose base name doesn't match the primary file name
        foreach (var typeDecl in topLevelTypes)
        {
            var baseName = typeDecl.Identifier.Text;
            if (string.IsNullOrEmpty(baseName))
                continue;

            if (!string.Equals(baseName, primaryName, StringComparison.Ordinal))
            {
                var diagnostic = Diagnostic.Create(
                    Rule,
                    typeDecl.Identifier.GetLocation(),
                    baseName,
                    fileName);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static bool IsGeneratedFile(string fileName)
    {
        // Skip *.g.cs, *.Generated.cs, *.designer.cs, GlobalUsings*.cs, AssemblyInfo.cs
        return fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".Generated.cs", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)
            || fileName.StartsWith("GlobalUsings", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, "AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase)
            || string.Equals(fileName, "AssemblyAttributes.cs", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetPrimaryFileName(string fileName)
    {
        // Remove the .cs extension first
        // "Foo.Part1.cs" -> "Foo.Part1"
        // "Foo.razor.cs" -> "Foo.razor"
        // "Foo.cs" -> "Foo"
        var withoutExtension = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrEmpty(withoutExtension))
            return string.Empty;

        // Take part before first '.'
        // "Foo.Part1" -> "Foo"
        // "Foo.razor" -> "Foo"
        // "Foo" -> "Foo"
        var dotIndex = withoutExtension.IndexOf('.');
        return dotIndex >= 0
            ? withoutExtension.Substring(0, dotIndex)
            : withoutExtension;
    }

    private static IEnumerable<BaseTypeDeclarationSyntax> GetTopLevelTypeDeclarations(SyntaxNode root)
    {
        // Walk the tree looking for type declarations that are NOT nested inside another type
        // BaseTypeDeclarationSyntax covers: class, struct, record, interface (TypeDeclarationSyntax) AND enum (EnumDeclarationSyntax)
        foreach (var node in root.DescendantNodes())
        {
            if (node is BaseTypeDeclarationSyntax typeDecl && !IsNestedType(typeDecl))
            {
                yield return typeDecl;
            }
        }
    }

    private static bool IsNestedType(BaseTypeDeclarationSyntax typeDecl)
    {
        // A type is nested if its parent is another type declaration
        var parent = typeDecl.Parent;
        while (parent != null)
        {
            if (parent is BaseTypeDeclarationSyntax)
                return true;

            // Stop at namespace or compilation unit
            if (parent is BaseNamespaceDeclarationSyntax || parent is CompilationUnitSyntax)
                return false;

            parent = parent.Parent;
        }

        return false;
    }
}
