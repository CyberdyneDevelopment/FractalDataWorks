using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Fdw.ServiceTypes.Analyzers;

/// <summary>
/// Analyzer that detects when a [TypeLookup] attribute on a base type property
/// will generate a method that conflicts with a member in the ServiceTypeCollection's inheritance chain.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TypeLookupNamingConflictAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    /// Diagnostic ID for TypeLookup naming conflicts.
    /// </summary>
    public const string DiagnosticId = "SVCTYPE001";

    private static readonly LocalizableString Title =
        "TypeLookup generates method that conflicts with collection member";
    private static readonly LocalizableString MessageFormat =
        "The [TypeLookup] on property '{0}' will generate a static method '{1}()' that hides the inherited member '{2}.{1}'.  Consider using a custom lookup name: [TypeLookup(\"By{0}\")].";
    private static readonly LocalizableString Description =
        "When a TypeLookup attribute generates a method with the same name as a member in the ServiceTypeCollection's inheritance chain, it causes CS0108 warnings. Use a custom lookup method name to avoid the conflict.";

    private const string Category = "ServiceTypes";

    private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [Rule];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeServiceTypeCollection, SymbolKind.NamedType);
    }

    private static void AnalyzeServiceTypeCollection(SymbolAnalysisContext context)
    {
        var typeSymbol = (INamedTypeSymbol)context.Symbol;

        // Check if this is a ServiceTypeCollection (has [ServiceTypeCollection] attribute)
        var serviceTypeCollectionAttr = typeSymbol.GetAttributes()
            .FirstOrDefault(ad => string.Equals(ad.AttributeClass?.Name, "ServiceTypeCollectionAttribute", System.StringComparison.Ordinal));

        if (serviceTypeCollectionAttr == null)
            return;

        // Get the base type from the ServiceTypeCollection attribute (first constructor argument)
        if (serviceTypeCollectionAttr.ConstructorArguments.Length == 0 ||
            serviceTypeCollectionAttr.ConstructorArguments[0].Value is not INamedTypeSymbol baseType)
            return;

        // Collect all members from the ServiceTypeCollection's inheritance chain
        var collectionMembers = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
        var currentType = typeSymbol.BaseType;
        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            foreach (var member in currentType.GetMembers())
            {
                if (!member.IsStatic && member.DeclaredAccessibility != Accessibility.Private)
                {
                    collectionMembers.Add(member.Name);
                }
            }
            currentType = currentType.BaseType;
        }

        // Walk the base type's inheritance chain looking for [TypeLookup] attributes
        currentType = baseType;
        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            foreach (var member in currentType.GetMembers())
            {
                if (member is IPropertySymbol property)
                {
                    var typeLookupAttr = property.GetAttributes()
                        .FirstOrDefault(ad => string.Equals(ad.AttributeClass?.Name, "TypeLookupAttribute", System.StringComparison.Ordinal));

                    if (typeLookupAttr != null)
                    {
                        // Get the method name (either from attribute argument or property name)
                        var methodName = property.Name;
                        if (typeLookupAttr.ConstructorArguments.Length > 0 &&
                            typeLookupAttr.ConstructorArguments[0].Value is string customName)
                        {
                            methodName = customName;
                        }

                        // Check if this method name conflicts with collection members
                        if (collectionMembers.Contains(methodName))
                        {
                            // Find the declaring type of the conflicting member
                            var conflictingMember = FindMemberInHierarchy(typeSymbol, methodName);
                            var conflictingTypeName = conflictingMember?.ContainingType.Name ?? "base class";

                            var diagnostic = Diagnostic.Create(
                                Rule,
                                property.Locations[0],
                                property.Name,
                                methodName,
                                conflictingTypeName);

                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
            currentType = currentType.BaseType;
        }
    }

    private static ISymbol? FindMemberInHierarchy(INamedTypeSymbol typeSymbol, string memberName)
    {
        var currentType = typeSymbol.BaseType;
        while (currentType != null && currentType.SpecialType != SpecialType.System_Object)
        {
            var member = currentType.GetMembers(memberName).FirstOrDefault();
            if (member != null)
                return member;
            currentType = currentType.BaseType;
        }
        return null;
    }
}
