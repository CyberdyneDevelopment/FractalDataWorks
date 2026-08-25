using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.ServiceTypes.Analyzers;

/// <summary>
/// Analyzer that detects and forbids the static Instance property pattern on TypeOption/ServiceType classes.
/// This pattern is incorrect and should never be used.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class InstancePropertyAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for the forbidden Instance property pattern.
    /// </summary>
    public const string DiagnosticId = "FDW025";

    private static readonly LocalizableString Title = "Singleton property pattern is forbidden";
    private static readonly LocalizableString MessageFormat = "Type '{0}' must not have a public static property '{1}' that returns the same type with '= new()'. Use collection access (e.g., MyTypes.ByName()) or direct instantiation instead.";
    private static readonly LocalizableString Description = @"The singleton property pattern (e.g., 'public static MyType Instance {{ get; }} = new();' or 'public static MyType Default {{ get; }} = new();') is explicitly forbidden.
This violates the TypeOption pattern architecture. Access instances via collection methods like MyTypes.ByName(), MyTypes.ById(), or MyTypes.All(), or use direct instantiation with 'new MyType()'.";
    private const string Category = "Critical";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: Description);

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
    }

    private static void AnalyzeProperty(SyntaxNodeAnalysisContext context)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration);

        if (propertySymbol == null)
            return;

        // Check if property is public
        if (propertySymbol.DeclaredAccessibility != Accessibility.Public)
            return;

        // Check if property is static
        if (!propertySymbol.IsStatic)
            return;

        // Get the containing type
        var containingType = propertySymbol.ContainingType;
        if (containingType == null)
            return;

        // Check if property type is the same as containing type
        if (!SymbolEqualityComparer.Default.Equals(propertySymbol.Type, containingType))
            return;

        // Check if property has initializer with "= new()"
        if (propertyDeclaration.Initializer == null)
            return;

        // Check if initializer is an object creation expression
        var initializerExpression = propertyDeclaration.Initializer.Value;
        if (initializerExpression is not (ImplicitObjectCreationExpressionSyntax or ObjectCreationExpressionSyntax))
            return;

        // Check if the containing type inherits from TypeOptionBase or ServiceTypeBase
        var typeOptionBase = context.Compilation.GetTypeByMetadataName("Fdw.Collections.TypeOptionBase`2");
        var serviceTypeBase = context.Compilation.GetTypeByMetadataName("Fdw.ServiceTypes.ServiceTypeBase`6");
        var serviceTypeBase5 = context.Compilation.GetTypeByMetadataName("Fdw.ServiceTypes.ServiceTypeBase`5");

        bool inheritsFromTypeOption = InheritsFromGenericType(containingType, typeOptionBase);
        bool inheritsFromServiceType = InheritsFromGenericType(containingType, serviceTypeBase) ||
                                        InheritsFromGenericType(containingType, serviceTypeBase5);

        // Also check if the type has [TypeOption] or [ServiceTypeOption] attribute
        var hasTypeOptionAttribute = containingType.GetAttributes()
            .Any(attr => string.Equals(attr.AttributeClass?.Name, "TypeOptionAttribute", StringComparison.Ordinal) ||
                         string.Equals(attr.AttributeClass?.Name, "TypeOption", StringComparison.Ordinal) ||
                         string.Equals(attr.AttributeClass?.Name, "ServiceTypeOptionAttribute", StringComparison.Ordinal) ||
                         string.Equals(attr.AttributeClass?.Name, "ServiceTypeOption", StringComparison.Ordinal));

        if (inheritsFromTypeOption || inheritsFromServiceType || hasTypeOptionAttribute)
        {
            // Report critical error - Singleton property pattern is forbidden
            var diagnostic = Diagnostic.Create(
                Rule,
                propertyDeclaration.GetLocation(),
                containingType.Name,
                propertySymbol.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool InheritsFromGenericType(INamedTypeSymbol type, INamedTypeSymbol? genericBaseType)
    {
        if (genericBaseType == null)
            return false;

        var currentType = type.BaseType;
        while (currentType != null)
        {
            if (currentType.OriginalDefinition.Equals(genericBaseType, SymbolEqualityComparer.Default))
                return true;

            currentType = currentType.BaseType;
        }

        return false;
    }
}
