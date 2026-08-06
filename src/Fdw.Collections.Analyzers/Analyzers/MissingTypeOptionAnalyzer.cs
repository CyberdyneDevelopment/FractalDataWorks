using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.Collections.Analyzers;

/// <summary>
/// Analyzer that warns when a type inherits from a base type specified in a [TypeCollection] attribute
/// but is missing the required [TypeOption] attribute needed for discovery by TypeCollectionGenerator.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingTypeOptionAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for missing TypeOption attribute on type option classes.
    /// </summary>
    public const string MissingTypeOptionDiagnosticId = "TC001";

    /// <summary>
    /// Diagnostic ID for TGeneric not matching defaultReturnType in TypeCollection attribute.
    /// </summary>
    public const string GenericTypeMismatchDiagnosticId = "TC002";

    /// <summary>
    /// Diagnostic ID for TBase not matching baseType in TypeCollection attribute.
    /// </summary>
    public const string BaseTypeMismatchDiagnosticId = "TC003";

    private static readonly LocalizableString MissingTypeOptionTitle = "Type option missing required [TypeOption] attribute";
    private static readonly LocalizableString MissingTypeOptionMessageFormat = "Type '{0}' inherits from '{1}' (used in TypeCollection) but is missing the required [TypeOption] attribute and will not be discovered";
    private static readonly LocalizableString MissingTypeOptionDescription = "Types that inherit from base types specified in [TypeCollection] attributes must have the [TypeOption] attribute to be discovered by TypeCollectionGenerator. Without this attribute, the type will be ignored during collection generation.";

    private static readonly LocalizableString GenericMismatchTitle = "TGeneric in base class doesn't match defaultReturnType in TypeCollection attribute";
    private static readonly LocalizableString GenericMismatchMessageFormat = "Collection '{0}' has TGeneric='{1}' in base class but defaultReturnType='{2}' in [TypeCollection] attribute. These must match.";
    private static readonly LocalizableString GenericMismatchDescription = "The TGeneric type parameter in TypeCollectionBase<TBase, TGeneric> must match the defaultReturnType parameter in the [TypeCollection] attribute for type safety.";

    private static readonly LocalizableString BaseMismatchTitle = "TBase in base class doesn't match baseType in TypeCollection attribute";
    private static readonly LocalizableString BaseMismatchMessageFormat = "Collection '{0}' has TBase='{1}' in base class but baseType='{2}' in [TypeCollection] attribute. These must match.";
    private static readonly LocalizableString BaseMismatchDescription = "The TBase type parameter in TypeCollectionBase<TBase, TGeneric> must match the baseType parameter in the [TypeCollection] attribute for type consistency.";

    private const string Category = "Usage";

    private static readonly DiagnosticDescriptor MissingTypeOptionRule = new DiagnosticDescriptor(
        MissingTypeOptionDiagnosticId,
        MissingTypeOptionTitle,
        MissingTypeOptionMessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: MissingTypeOptionDescription,
        customTags: new[] { "CompilationEnd" });

    private static readonly DiagnosticDescriptor GenericMismatchRule = new DiagnosticDescriptor(
        GenericTypeMismatchDiagnosticId,
        GenericMismatchTitle,
        GenericMismatchMessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: GenericMismatchDescription);

    private static readonly DiagnosticDescriptor BaseMismatchRule = new DiagnosticDescriptor(
        BaseTypeMismatchDiagnosticId,
        BaseMismatchTitle,
        BaseMismatchMessageFormat,
        Category,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: BaseMismatchDescription);

    /// <summary>
    /// Gets the supported diagnostic descriptors for this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MissingTypeOptionRule, GenericMismatchRule, BaseMismatchRule);

    /// <summary>
    /// Initializes the analyzer by registering compilation analysis actions.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        // Get required attribute types
        var typeCollectionAttribute = context.Compilation.GetTypeByMetadataName("Fdw.Collections.Attributes.TypeCollectionAttribute");
        var typeOptionAttribute = context.Compilation.GetTypeByMetadataName("Fdw.Collections.Attributes.TypeOptionAttribute");

        if (typeCollectionAttribute == null || typeOptionAttribute == null)
            return; // Attributes not available in this compilation

        // Step 1: Find all base types specified in [TypeCollection] attributes
        var collectionBaseTypes = FindCollectionBaseTypes(context.Compilation, typeCollectionAttribute);

        // Step 2: For each base type, find inheriting types and check for [TypeOption]
        foreach (var baseType in collectionBaseTypes)
        {
            CheckInheritingTypesForTypeOption(context, baseType, typeOptionAttribute);
        }
    }

    /// <summary>
    /// Finds all base types specified in [TypeCollection] attributes across the compilation.
    /// </summary>
    private static ImmutableArray<INamedTypeSymbol> FindCollectionBaseTypes(Compilation compilation, INamedTypeSymbol typeCollectionAttribute)
    {
        var baseTypes = ImmutableArray.CreateBuilder<INamedTypeSymbol>();

        // Scan all types for [TypeCollection] attributes
        ScanNamespaceForCollections(compilation.GlobalNamespace, typeCollectionAttribute, baseTypes);

        return baseTypes.ToImmutable();
    }

    /// <summary>
    /// Recursively scans namespaces for types with [TypeCollection] attributes and extracts their base types.
    /// </summary>
    private static void ScanNamespaceForCollections(INamespaceSymbol namespaceSymbol, INamedTypeSymbol typeCollectionAttribute, ImmutableArray<INamedTypeSymbol>.Builder baseTypes)
    {
        // Check types in current namespace
        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            var attribute = type.GetAttributes()
                .FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, typeCollectionAttribute));

            if (attribute?.ConstructorArguments.Length > 0 &&
                attribute.ConstructorArguments[0].Value is ITypeSymbol baseTypeSymbol &&
                baseTypeSymbol is INamedTypeSymbol namedBaseType)
            {
                baseTypes.Add(namedBaseType);
            }

            // Recursively scan nested types
            ScanNestedTypesForCollections(type, typeCollectionAttribute, baseTypes);
        }

        // Recursively scan child namespaces
        foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            ScanNamespaceForCollections(childNamespace, typeCollectionAttribute, baseTypes);
        }
    }

    /// <summary>
    /// Recursively scans nested types for [TypeCollection] attributes.
    /// </summary>
    private static void ScanNestedTypesForCollections(INamedTypeSymbol parentType, INamedTypeSymbol typeCollectionAttribute, ImmutableArray<INamedTypeSymbol>.Builder baseTypes)
    {
        foreach (var nestedType in parentType.GetTypeMembers())
        {
            var attribute = nestedType.GetAttributes()
                .FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, typeCollectionAttribute));

            if (attribute?.ConstructorArguments.Length > 0 &&
                attribute.ConstructorArguments[0].Value is ITypeSymbol baseTypeSymbol &&
                baseTypeSymbol is INamedTypeSymbol namedBaseType)
            {
                baseTypes.Add(namedBaseType);
            }

            // Recursively scan deeper nested types
            ScanNestedTypesForCollections(nestedType, typeCollectionAttribute, baseTypes);
        }
    }

    /// <summary>
    /// Checks all types that inherit from the specified base type for the [TypeOption] attribute.
    /// </summary>
    private static void CheckInheritingTypesForTypeOption(CompilationAnalysisContext context, INamedTypeSymbol baseType, INamedTypeSymbol typeOptionAttribute)
    {
        // Scan current compilation for inheriting types
        CheckNamespaceForInheritingTypes(context, context.Compilation.GlobalNamespace, baseType, typeOptionAttribute);

        // Also scan referenced assemblies
        foreach (var reference in context.Compilation.References)
        {
            if (context.Compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assemblySymbol)
            {
                CheckNamespaceForInheritingTypes(context, assemblySymbol.GlobalNamespace, baseType, typeOptionAttribute);
            }
        }
    }

    /// <summary>
    /// Recursively checks a namespace for types that inherit from the base type but lack [TypeOption].
    /// </summary>
    private static void CheckNamespaceForInheritingTypes(CompilationAnalysisContext context, INamespaceSymbol namespaceSymbol, INamedTypeSymbol baseType, INamedTypeSymbol typeOptionAttribute)
    {
        // Check types in current namespace
        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            if (InheritsFromBaseType(type, baseType) && !HasTypeOptionAttribute(type, typeOptionAttribute))
            {
                // Only report for types in the current compilation (not referenced assemblies)
                if (SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, context.Compilation.Assembly))
                {
                    ReportMissingTypeOption(context, type, baseType);
                }
            }

            // Recursively check nested types
            CheckNestedTypesForInheritance(context, type, baseType, typeOptionAttribute);
        }

        // Recursively check child namespaces
        foreach (var childNamespace in namespaceSymbol.GetNamespaceMembers())
        {
            CheckNamespaceForInheritingTypes(context, childNamespace, baseType, typeOptionAttribute);
        }
    }

    /// <summary>
    /// Recursively checks nested types for inheritance and missing [TypeOption].
    /// </summary>
    private static void CheckNestedTypesForInheritance(CompilationAnalysisContext context, INamedTypeSymbol parentType, INamedTypeSymbol baseType, INamedTypeSymbol typeOptionAttribute)
    {
        foreach (var nestedType in parentType.GetTypeMembers())
        {
            if (InheritsFromBaseType(nestedType, baseType) && !HasTypeOptionAttribute(nestedType, typeOptionAttribute))
            {
                // Only report for types in the current compilation
                if (SymbolEqualityComparer.Default.Equals(nestedType.ContainingAssembly, context.Compilation.Assembly))
                {
                    ReportMissingTypeOption(context, nestedType, baseType);
                }
            }

            // Recursively check deeper nested types
            CheckNestedTypesForInheritance(context, nestedType, baseType, typeOptionAttribute);
        }
    }

    /// <summary>
    /// Determines whether a type is the collection's source-generated <c>NotFound</c> sentinel.
    /// </summary>
    /// <remarks>
    /// The TypeCollection generators emit a concrete <c>NotFound{Collection}</c> sentinel that inherits
    /// the option base but is NOT a registrable option (it is the miss-return value, not a member). A
    /// host may add a user-authored partial to give it behaviour — that hand-written declaration would
    /// otherwise trip this analyzer, a false positive. The sentinel is uniquely identifiable because it
    /// ALWAYS has at least one source-generated declaration (the generated <c>{Collection}.g.cs</c> part);
    /// a genuinely-missing-[TypeOption] option class never has a generated part.
    /// </remarks>
    private static bool IsGeneratedSentinel(INamedTypeSymbol type)
        => type.DeclaringSyntaxReferences.Any(r =>
        {
            var path = r.SyntaxTree.FilePath;
            return path != null && path.EndsWith(".g.cs", System.StringComparison.OrdinalIgnoreCase);
        });

    /// <summary>
    /// Checks if a type inherits from the specified base type.
    /// </summary>
    private static bool InheritsFromBaseType(INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        // Skip abstract types and interfaces
        if (type.IsAbstract || type.TypeKind == TypeKind.Interface)
            return false;

        // Skip the source-generated NotFound sentinel — it inherits the base but is not a registrable option.
        if (IsGeneratedSentinel(type))
            return false;

        // Check inheritance chain
        var currentType = type.BaseType;
        while (currentType != null)
        {
            if (SymbolEqualityComparer.Default.Equals(currentType, baseType))
                return true;

            currentType = currentType.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Checks if a type has the [TypeOption] attribute.
    /// </summary>
    private static bool HasTypeOptionAttribute(INamedTypeSymbol type, INamedTypeSymbol typeOptionAttribute)
    {
        return type.GetAttributes()
            .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, typeOptionAttribute));
    }

    /// <summary>
    /// Reports a diagnostic for a type missing the [TypeOption] attribute.
    /// </summary>
    private static void ReportMissingTypeOption(CompilationAnalysisContext context, INamedTypeSymbol type, INamedTypeSymbol baseType)
    {
        // Find the class declaration syntax for better location reporting
        var syntaxReferences = type.DeclaringSyntaxReferences;
        if (syntaxReferences.Length > 0)
        {
            var syntaxNode = syntaxReferences[0].GetSyntax();
            if (syntaxNode is ClassDeclarationSyntax classDeclaration)
            {
                var diagnostic = Diagnostic.Create(
                    MissingTypeOptionRule,
                    classDeclaration.Identifier.GetLocation(),
                    type.Name,
                    baseType.Name);

                context.ReportDiagnostic(diagnostic);
                return;
            }
        }

        // Fallback to no location if we can't find the syntax
        var fallbackDiagnostic = Diagnostic.Create(
            MissingTypeOptionRule,
            Location.None,
            type.Name,
            baseType.Name);

        context.ReportDiagnostic(fallbackDiagnostic);
    }
}