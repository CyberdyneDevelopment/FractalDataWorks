using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Collections.Analyzers;

/// <summary>
/// Analyzer that enforces ITypeOption implementation on enhanced enum base classes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EnhancedEnumBaseAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for enhanced enum base classes that should implement ITypeOption.
    /// </summary>
    public const string DiagnosticId = "FDW042";

    private static readonly LocalizableString Title = "Enhanced enum base class should implement ITypeOption";
    private static readonly LocalizableString MessageFormat = "Enhanced enum base class '{0}' should implement ITypeOption for full functionality";
    private static readonly LocalizableString Description = "Enhanced enum base classes should implement ITypeOption to enable features like GetById generation and proper interface-based return types.";
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

        // Check if class has [EnumOptionBase] attribute
        var hasEnhancedEnumBaseAttribute = classSymbol.GetAttributes()
            .Any(attr => string.Equals(attr.AttributeClass?.Name, "EnhancedEnumBaseAttribute", StringComparison.Ordinal) ||
                         string.Equals(attr.AttributeClass?.Name, "EnumOptionBase", StringComparison.Ordinal));

        if (!hasEnhancedEnumBaseAttribute)
            return;

        // Check if class implements ITypeOption
        var typeOptionInterface = context.Compilation.GetTypeByMetadataName("Fdw.Collections.ITypeOption");
        if (typeOptionInterface == null)
        {
            // If the interface isn't available in the compilation, we can't check
            return;
        }

        var implementsInterface = classSymbol.AllInterfaces.Contains(typeOptionInterface, SymbolEqualityComparer.Default);

        if (!implementsInterface)
        {
            // Report diagnostic
            var diagnostic = Diagnostic.Create(
                Rule,
                classDeclaration.Identifier.GetLocation(),
                classSymbol.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
