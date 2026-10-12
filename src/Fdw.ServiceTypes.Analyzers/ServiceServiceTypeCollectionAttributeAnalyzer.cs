using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.ServiceTypes.Analyzers;

/// <summary>
/// Analyzer that ensures EnumCollection attribute has CollectionName specified and validates inheritance.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ServiceServiceTypeCollectionAttributeAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for missing CollectionName in EnumCollection attribute.
    /// </summary>
    public const string MissingCollectionNameDiagnosticId = "FDW030";

    /// <summary>
    /// Diagnostic ID for EnumCollection classes not inheriting from EnumOptionBase&lt;T&gt;.
    /// </summary>
    public const string MissingInheritanceDiagnosticId = "FDW031";

    /// <summary>
    /// Diagnostic ID for generic EnumCollection classes that don't specify an interface for T.
    /// </summary>
    public const string GenericMustUseInterfaceDiagnosticId = "FDW032";

    private static readonly LocalizableString MissingCollectionNameTitle = "EnumCollection attribute must specify CollectionName";
    private static readonly LocalizableString MissingCollectionNameMessageFormat = "EnumCollection attribute on class '{0}' must explicitly specify the CollectionName parameter";
    private static readonly LocalizableString MissingCollectionNameDescription = "Enhanced enum collection classes must explicitly specify the CollectionName in the EnumCollection attribute for clarity and consistency.";

    private static readonly LocalizableString MissingInheritanceTitle = "EnumCollection class must inherit from EnumOptionBase<T> or ServiceServiceTypeCollectionBase<T>";
    private static readonly LocalizableString MissingInheritanceMessageFormat = "Class '{0}' with EnumCollection attribute must inherit from EnumOptionBase<T> or ServiceServiceTypeCollectionBase<T>";
    private static readonly LocalizableString MissingInheritanceDescription = "Classes marked with EnumCollection attribute must inherit from EnumOptionBase<T> or ServiceServiceTypeCollectionBase<T> to provide the required functionality.";

    private static readonly LocalizableString GenericMustUseInterfaceTitle = "Generic EnumCollection must specify a non-generic interface constraint for T";
    private static readonly LocalizableString GenericMustUseInterfaceMessageFormat = "Generic EnumCollection class '{0}' must specify a non-generic interface constraint for type parameter T (e.g., where T : IYourInterface, {0}<T>)";
    private static readonly LocalizableString GenericMustUseInterfaceDescription = "Generic EnumCollection classes require a non-generic interface constraint for T to ensure proper type resolution in the generated ServiceTypes. The interface cannot be generic.";

    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor MissingCollectionNameRule = new DiagnosticDescriptor(
        MissingCollectionNameDiagnosticId,
        MissingCollectionNameTitle,
        MissingCollectionNameMessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: MissingCollectionNameDescription);

    private static readonly DiagnosticDescriptor MissingInheritanceRule = new DiagnosticDescriptor(
        MissingInheritanceDiagnosticId,
        MissingInheritanceTitle,
        MissingInheritanceMessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: MissingInheritanceDescription);

    private static readonly DiagnosticDescriptor GenericMustUseInterfaceRule = new DiagnosticDescriptor(
        GenericMustUseInterfaceDiagnosticId,
        GenericMustUseInterfaceTitle,
        GenericMustUseInterfaceMessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: GenericMustUseInterfaceDescription);

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [MissingCollectionNameRule, MissingInheritanceRule, GenericMustUseInterfaceRule];

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
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration);

        if (classSymbol == null)
            return;

        // Find EnumCollection attributes
        var enumCollectionAttributes = classSymbol.GetAttributes()
            .Where(attr => string.Equals(attr.AttributeClass?.Name, "ServiceServiceTypeCollectionAttribute", StringComparison.Ordinal) ||
                          string.Equals(attr.AttributeClass?.Name, "EnumCollection", StringComparison.Ordinal))
            .ToList();

        if (enumCollectionAttributes.Count == 0)
            return;

        // Check each EnumCollection attribute
        foreach (var attr in enumCollectionAttributes)
        {
            // Check if CollectionName is specified
            if (!HasCollectionNameSpecified(attr))
            {
                var diagnostic = Diagnostic.Create(
                    MissingCollectionNameRule,
                    classDeclaration.Identifier.GetLocation(),
                    classSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }

        // Check inheritance from EnumOptionBase<T>
        if (!InheritsFromEnumOptionBase(classSymbol))
        {
            var diagnostic = Diagnostic.Create(
                MissingInheritanceRule,
                classDeclaration.Identifier.GetLocation(),
                classSymbol.Name);

            context.ReportDiagnostic(diagnostic);
        }

        // Check if this is a generic class that needs an interface constraint
        if (HasGenericAttribute(enumCollectionAttributes) && !HasValidInterfaceConstraint(classSymbol))
        {
            var diagnostic = Diagnostic.Create(
                GenericMustUseInterfaceRule,
                classDeclaration.Identifier.GetLocation(),
                classSymbol.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool HasCollectionNameSpecified(AttributeData attr)
    {
        // Check constructor arguments first (primary way to specify CollectionName)
        if (attr.ConstructorArguments.Length > 0)
        {
            var firstArg = attr.ConstructorArguments[0];
            if (firstArg.Value is string collectionName && !string.IsNullOrEmpty(collectionName))
            {
                return true;
            }
        }

        // Check named arguments as fallback
        foreach (var namedArg in attr.NamedArguments)
        {
            if (string.Equals(namedArg.Key, "CollectionName", StringComparison.Ordinal) &&
                namedArg.Value.Value is string namedCollectionName && !string.IsNullOrEmpty(namedCollectionName))
            {
                return true;
            }
        }

        return false;
    }

    private static bool InheritsFromEnumOptionBase(INamedTypeSymbol classSymbol)
    {
        var current = classSymbol.BaseType;

        while (current != null)
        {
            if (current.IsGenericType)
            {
                var originalDefinition = current.OriginalDefinition;

                // Check if this is EnumOptionBase<T> or ServiceServiceTypeCollectionBase<T>
                // Use the metadata name for proper type comparison
                var metadataName = originalDefinition.MetadataName;
                var namespaceName = originalDefinition.ContainingNamespace?.ToDisplayString();

                if (string.Equals(namespaceName, "Fdw.ServiceTypes", StringComparison.Ordinal) &&
                    (string.Equals(metadataName, "EnumOptionBase`1", StringComparison.Ordinal) ||
                     string.Equals(metadataName, "ServiceServiceTypeCollectionBase`1", StringComparison.Ordinal)))
                {
                    return true;
                }
            }

            current = current.BaseType;
        }

        return false;
    }

    private static bool HasGenericAttribute(List<AttributeData> enumCollectionAttributes)
    {
        foreach (var attr in enumCollectionAttributes)
        {
            // Check for Generic named argument
            var genericArg = attr.NamedArguments
                .FirstOrDefault(arg => string.Equals(arg.Key, "Generic", StringComparison.Ordinal));

            if (genericArg.Value.Value is bool isGeneric && isGeneric)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasValidInterfaceConstraint(INamedTypeSymbol classSymbol)
    {
        // Check if this is a generic class that inherits from EnumOptionBase<T> or ServiceServiceTypeCollectionBase<T>
        if (!classSymbol.IsGenericType)
        {
            return true; // Non-generic classes don't need interface constraints
        }

        // Find the base type that is EnumOptionBase<T> or ServiceServiceTypeCollectionBase<T>
        var current = classSymbol.BaseType;
        while (current != null)
        {
            if (current.IsGenericType)
            {
                var metadataName = current.OriginalDefinition.MetadataName;
                var namespaceName = current.OriginalDefinition.ContainingNamespace?.ToDisplayString();

                if (string.Equals(namespaceName, "Fdw.ServiceTypes", StringComparison.Ordinal) &&
                    (string.Equals(metadataName, "EnumOptionBase`1", StringComparison.Ordinal) ||
                     string.Equals(metadataName, "ServiceServiceTypeCollectionBase`1", StringComparison.Ordinal)))
                {
                    // For ServiceServiceTypeCollectionBase, we don't need to check for interface constraints
                    // as it's a different pattern (collection vs enum values)
                    if (string.Equals(metadataName, "ServiceServiceTypeCollectionBase`1", StringComparison.Ordinal))
                    {
                        return true; // ServiceServiceTypeCollectionBase doesn't require interface constraints
                    }

                    // Check if the type parameter T has an interface constraint (for EnumOptionBase)
                    if (current.TypeArguments.Length > 0)
                    {
                        var typeParameter = current.TypeArguments[0];

                        // If the type parameter is the same as the class itself, we need to check constraints
                        if (SymbolEqualityComparer.Default.Equals(typeParameter, classSymbol))
                        {
                            // Find the corresponding type parameter in the class definition
                            var classTypeParam = classSymbol.TypeParameters.FirstOrDefault();
                            if (classTypeParam != null)
                            {
                                // Check if it has non-generic interface constraints
                                foreach (var constraint in classTypeParam.ConstraintTypes)
                                {
                                    if (constraint.TypeKind == TypeKind.Interface)
                                    {
                                        // Check if the interface is non-generic
                                        if (constraint is INamedTypeSymbol namedConstraint && !namedConstraint.IsGenericType)
                                        {
                                            return true; // Found a non-generic interface constraint
                                        }
                                    }
                                }

                                // No interface constraint found
                                return false;
                            }
                        }
                    }
                    break;
                }
            }
            current = current.BaseType;
        }

        // If we can't determine the constraint structure, assume it's valid
        return true;
    }
}

