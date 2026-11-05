using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.ServiceTypes.Analyzers;

/// <summary>
/// Analyzer that ensures every cross-assembly <c>[TypeOption]</c>- or <c>[Implementation]</c>-tagged class has a
/// public parameterless constructor. <c>[Implementation]</c> options are registered the same way, by
/// <c>Fdw.Registration.SourceGenerators.ImplementationModuleInitializerGenerator</c>, which skips them silently too
/// (FDW-3512). The cross-assembly TypeOption module initializer
/// (<c>Fdw.Registration.SourceGenerators.TypeOptionModuleInitializerGenerator</c>) registers every
/// tagged option with a bare <c>new()</c> call in the consuming executable — before any DI
/// container exists — and silently skips (no diagnostic) any class that lacks a public
/// parameterless constructor. A skipped class is never registered into its TypeCollection, so any
/// runtime lookup for it fails with "not found" instead of failing at compile time.
/// </summary>
/// <remarks>
/// <c>RestrictToCurrentCompilation = true</c> options are exempt: they are registered instead by
/// the TypeCollection's own same-compilation static constructor (<c>Fdw.Collections.SourceGenerators
/// .TypeCollectionGenerator</c>), which emits <c>new T()</c> directly against whatever constructor
/// the class declares — an all-optional-parameter constructor (e.g. the standard
/// <c>ILogger? logger = null</c> pattern) compiles and works there, and a genuinely-required
/// parameter with no default is already caught as a compiler error at that call site, not silently
/// dropped. Confirmed empirically: TriggerTypes' Cron/Interval/Manual/Once/Window (all
/// RestrictToCurrentCompilation = true, several with only an optional-logger constructor) resolve
/// correctly at runtime today.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TypeOptionConstructorAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for a [TypeOption] class missing a public parameterless constructor.
    /// </summary>
    public const string DiagnosticId = "FDW027";

    private static readonly LocalizableString Title = "Missing public parameterless constructor";
    private static readonly LocalizableString MessageFormat = "{0} '{1}' must have a public parameterless constructor — the module initializer registers it via a bare new() call and will silently skip it otherwise";
    private static readonly LocalizableString Description = "Every [TypeOption]- or [Implementation]-tagged class is instantiated via a bare new() by its module initializer, before any DI container exists. A class without a public parameterless constructor is silently skipped — never registered — with no build signal.";
    private const string Category = "Usage";

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

        context.RegisterSyntaxNodeAction(AnalyzeType, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.RecordDeclaration);
    }

    private static void AnalyzeType(SyntaxNodeAnalysisContext context)
    {
        var typeDeclaration = (TypeDeclarationSyntax)context.Node;
        var typeSymbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration, context.CancellationToken);
        if (typeSymbol == null) return;

        // Check if this type carries [TypeOption(...)] (Fdw.Collections.Attributes.TypeOptionAttribute) or
        // [Implementation(...)] (Fdw.Collections.ImplementationAttribute).
        var optionAttr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => string.Equals(a.AttributeClass?.Name, "TypeOptionAttribute", StringComparison.Ordinal) ||
                                 string.Equals(a.AttributeClass?.Name, "ImplementationAttribute", StringComparison.Ordinal));

        if (optionAttr == null) return;

        var isImplementation = string.Equals(optionAttr.AttributeClass?.Name, "ImplementationAttribute", StringComparison.Ordinal);

        // RestrictToCurrentCompilation = true options are registered by a different mechanism
        // (the collection's own same-compilation static constructor) that does not require a
        // strictly zero-parameter constructor — see the class remarks. Not this analyzer's concern.
        // A [TypeOption] carries the flag itself; an [Implementation] has no such property, so the flag is read
        // from the collection attribute on the type its first argument names.
        var isRestrictedToCurrentCompilation = isImplementation
            ? IsCollectionRestrictedToCurrentCompilation(optionAttr)
            : HasRestrictToCurrentCompilation(optionAttr);
        if (isRestrictedToCurrentCompilation) return;

        // The module initializer itself skips generic and abstract types before ever reaching the
        // constructor check (neither can be instantiated via new()) — mirror that here so this
        // analyzer only fires on the same classes the generator would actually attempt to register.
        if (typeSymbol.IsGenericType || typeSymbol.TypeParameters.Length > 0) return;
        if (typeSymbol.IsAbstract) return;

        var hasPublicParameterlessConstructor = typeSymbol.Constructors
            .Any(c => !c.IsStatic &&
                     c.DeclaredAccessibility == Accessibility.Public &&
                     c.Parameters.Length == 0);

        if (!hasPublicParameterlessConstructor)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                typeDeclaration.Identifier.GetLocation(),
                isImplementation ? "Implementation" : "TypeOption",
                typeSymbol.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool HasRestrictToCurrentCompilation(AttributeData attribute)
    {
        return attribute.NamedArguments
            .Any(a => string.Equals(a.Key, "RestrictToCurrentCompilation", StringComparison.Ordinal) &&
                     a.Value.Value is true);
    }

    private static bool IsCollectionRestrictedToCurrentCompilation(AttributeData implementationAttribute)
    {
        if (implementationAttribute.ConstructorArguments.Length == 0 ||
            implementationAttribute.ConstructorArguments[0].Value is not INamedTypeSymbol collectionType)
            return false;

        // The service collection attributes (ServiceTypeCollection, MutableServiceTypeCollection,
        // ServiceTypeInstanceCollection) each carry RestrictToCurrentCompilation.
        return collectionType.GetAttributes()
            .Any(a => a.AttributeClass?.Name.EndsWith("CollectionAttribute", StringComparison.Ordinal) == true &&
                      HasRestrictToCurrentCompilation(a));
    }
}
