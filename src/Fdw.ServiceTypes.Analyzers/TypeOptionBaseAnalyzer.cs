using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.ServiceTypes.Analyzers;

/// <summary>
/// Analyzer that enforces ITypeOption implementation on type option base classes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TypeOptionBaseAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for enhanced enum base classes that should implement ITypeOption.
    /// </summary>
    public const string DiagnosticId = "FDW033";

    private static readonly LocalizableString Title = "Type option base class should implement ITypeOption";
    private static readonly LocalizableString MessageFormat = "Type option base class '{0}' should implement ITypeOption for full functionality";
    private static readonly LocalizableString Description = "Type option base classes should implement ITypeOption to enable features like GetById generation and proper interface-based return types.";
    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
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
        context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClass(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);

        if (classSymbol == null)
            return;

        // Check if class has [TypeOptionBase] attribute
        var hasTypeOptionBaseAttribute = classSymbol.GetAttributes()
            .Any(attr => string.Equals(attr.AttributeClass?.Name, "TypeOptionBaseAttribute", StringComparison.Ordinal) ||
                         string.Equals(attr.AttributeClass?.Name, "TypeOptionBase", StringComparison.Ordinal) ||
                         string.Equals(attr.AttributeClass?.Name, "EnhancedEnumBaseAttribute", StringComparison.Ordinal) ||
                         string.Equals(attr.AttributeClass?.Name, "EnumOptionBaseAttribute", StringComparison.Ordinal) ||
                         string.Equals(attr.AttributeClass?.Name, "EnumOptionBase", StringComparison.Ordinal));

        if (!hasTypeOptionBaseAttribute)
            return;

        // Check if class implements ITypeOption
        var typeOptionInterface = context.Compilation.GetTypeByMetadataName("Fdw.Collections.ITypeOption");

        // If the interface doesn't exist in the compilation, we can't check - don't report diagnostic
        if (typeOptionInterface == null)
        {
            return;
        }

        bool implementsInterface = classSymbol.AllInterfaces.Contains(typeOptionInterface, SymbolEqualityComparer.Default);

        if (!implementsInterface)
        {
            // Report diagnostic - class doesn't implement required interface
            var diagnostic = Diagnostic.Create(
                Rule,
                classDeclaration.Identifier.GetLocation(),
                classSymbol.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }
}

