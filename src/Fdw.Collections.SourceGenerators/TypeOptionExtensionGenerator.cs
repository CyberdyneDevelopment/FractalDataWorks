using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Fdw.Conventions;

namespace Fdw.Collections.SourceGenerators;

/// <summary>
/// Generates C# 14 static extension methods for TypeOption and ServiceTypeOption classes.
/// Each TypeOption gets extension methods on its collection class for type-safe access.
/// </summary>
/// <remarks>
/// <para>
/// For singleton collections (TypeCollection, ServiceTypeCollection, MutableTypeCollection, MutableServiceTypeCollection):
/// - Parameterless method returns singleton from collection.ById(generatedId)
/// - Parameterized overloads return new instances
/// </para>
/// <para>
/// For factory collections (TypeInstanceCollection, ServiceTypeInstanceCollection):
/// - All methods return new instances via new TypeOptionType(...)
/// </para>
/// </remarks>
[Generator]
public class TypeOptionExtensionGenerator : IIncrementalGenerator
{
    // TypeOption attribute names (both Collections and ServiceTypes.Attributes namespaces)
    private static readonly HashSet<string> TypeOptionAttributeNames = new(StringComparer.Ordinal)
    {
        "Fdw.Collections.TypeOptionAttribute",
        "Fdw.Collections.Attributes.TypeOptionAttribute"
    };

    private static readonly HashSet<string> ServiceTypeOptionAttributeNames = new(StringComparer.Ordinal)
    {
        "Fdw.Collections.ServiceTypeOptionAttribute",
        "Fdw.Collections.Attributes.ServiceTypeOptionAttribute",
        "Fdw.ServiceTypes.Attributes.ServiceTypeOptionAttribute"
    };

    // Collection attribute names for determining CollectionKind
    private static readonly HashSet<string> SingletonCollectionAttributes = new(StringComparer.Ordinal)
    {
        "Fdw.Collections.TypeCollectionAttribute",
        "Fdw.Collections.ServiceTypeCollectionAttribute",
        "Fdw.Collections.Attributes.TypeCollectionAttribute",
        "Fdw.Collections.Attributes.ServiceTypeCollectionAttribute",
        "Fdw.ServiceTypes.Attributes.ServiceTypeCollectionAttribute"
    };

    private static readonly HashSet<string> MutableCollectionAttributes = new(StringComparer.Ordinal)
    {
        "Fdw.Collections.MutableTypeCollectionAttribute",
        "Fdw.Collections.MutableServiceTypeCollectionAttribute",
        "Fdw.Collections.Attributes.MutableTypeCollectionAttribute",
        "Fdw.Collections.Attributes.MutableServiceTypeCollectionAttribute"
    };

    private static readonly HashSet<string> FactoryCollectionAttributes = new(StringComparer.Ordinal)
    {
        "Fdw.Collections.TypeInstanceCollectionAttribute",
        "Fdw.Collections.ServiceTypeInstanceCollectionAttribute",
        "Fdw.Collections.Attributes.TypeInstanceCollectionAttribute",
        "Fdw.Collections.Attributes.ServiceTypeInstanceCollectionAttribute"
    };

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Discover [TypeOption] classes
        var typeOptionsProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0,
                transform: (ctx, ct) => ExtractOptionModel(ctx, isServiceType: false, ct))
            .Where(static m => m != null)
            .Select(static (m, _) => m!.Value);

        // Only use TypeOptions for now
        var allOptionsProvider = typeOptionsProvider.Collect();

        // Generate extension for each option
        context.RegisterSourceOutput(allOptionsProvider, Execute);
    }

#pragma warning disable MA0051 // Source generator model extraction requires cohesive Roslyn symbol inspection
    private static ExtensionOptionModel? ExtractOptionModel(
        GeneratorSyntaxContext context,
        bool isServiceType,
        CancellationToken ct)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl, ct) as INamedTypeSymbol;

        if (classSymbol == null)
            return null;

        // Find the TypeOption or ServiceTypeOption attribute
        var attributeNames = isServiceType ? ServiceTypeOptionAttributeNames : TypeOptionAttributeNames;
        AttributeData? attribute = null;

        foreach (var attr in classSymbol.GetAttributes())
        {
            var attrFullName = attr.AttributeClass?.ToDisplayString();
            if (attrFullName != null && attributeNames.Contains(attrFullName))
            {
                attribute = attr;
                break;
            }
        }

        if (attribute == null)
            return null;

        // Skip generic types
        if (classSymbol.IsGenericType || classSymbol.TypeParameters.Length > 0)
            return null;

        // Need at least collectionType and name
        if (attribute.ConstructorArguments.Length < 2)
            return null;

        var collectionType = attribute.ConstructorArguments[0].Value as ITypeSymbol;
        var optionName = attribute.ConstructorArguments[1].Value?.ToString() ?? "";

        // Third argument is optional methodReturnType
        ITypeSymbol? methodReturnType = null;
        if (attribute.ConstructorArguments.Length >= 3)
        {
            methodReturnType = attribute.ConstructorArguments[2].Value as ITypeSymbol;
        }

        if (collectionType == null || string.IsNullOrEmpty(optionName))
            return null;

        // Determine collection kind by checking its attributes
        var (collectionKind, isGuidBased) = DetermineCollectionKind(collectionType);

        // Extract constructors
        var constructors = ExtractConstructors(classSymbol);

        var generatedId = GenerateIdFromName(GetClrFullName(classSymbol));

        // Skip extension generation for Guid-based collections
        // Guid-based collections (like DataStoreTypes) use computed Guid IDs,
        // and we can't generate a stable Guid from a name for ById lookups
        if (isGuidBased)
        {
            return null;
        }

        return new ExtensionOptionModel(
            TypeName: classSymbol.Name,
            FullTypeName: classSymbol.ToDisplayString(),
            Namespace: classSymbol.ContainingNamespace.ToDisplayString(),
            CollectionFullName: collectionType.ToDisplayString(),
            CollectionClassName: collectionType.Name,
            OptionName: optionName,
            GeneratedId: generatedId,
            Constructors: constructors,
            MethodReturnTypeName: methodReturnType?.ToDisplayString(),
            CollectionKind: collectionKind,
            IsServiceType: isServiceType,
            IsGuidBasedCollection: isGuidBased
        );
    }

    private static (CollectionKind Kind, bool IsGuidBased) DetermineCollectionKind(ITypeSymbol collectionType)
    {
        if (collectionType is not INamedTypeSymbol namedType)
            return (CollectionKind.Singleton, false);

        var collectionKind = CollectionKind.Singleton;
        var isGuidBased = false;

        // Check attributes to determine collection kind
        foreach (var attr in namedType.GetAttributes())
        {
            var attrFullName = attr.AttributeClass?.ToDisplayString();
            if (attrFullName == null) continue;

            // ServiceTypeCollection always uses Guid-based IDs
            if (attrFullName.Contains("ServiceTypeCollection"))
            {
                collectionKind = CollectionKind.Singleton;
                isGuidBased = true;
                break;
            }

            if (SingletonCollectionAttributes.Contains(attrFullName))
            {
                collectionKind = CollectionKind.Singleton;
                break;
            }

            if (MutableCollectionAttributes.Contains(attrFullName))
            {
                collectionKind = CollectionKind.Mutable;
                break;
            }

            if (FactoryCollectionAttributes.Contains(attrFullName))
            {
                collectionKind = CollectionKind.Factory;
                break;
            }
        }

        // Check if the collection's base type uses Guid IDs by examining the TBase type argument
        // TypeCollectionBase<TBase, TInterface> where TBase : TypeOptionBase<TId, TSelf>
        if (!isGuidBased)
        {
            isGuidBased = IsGuidBasedCollection(namedType);
        }

        return (collectionKind, isGuidBased);
    }

    private static bool IsGuidBasedCollection(INamedTypeSymbol collectionType)
    {
        // Walk up the base type hierarchy to find TypeCollectionBase or ServiceTypeCollectionBase
        var currentType = collectionType.BaseType;
        while (currentType != null)
        {
            var typeName = currentType.Name;
            if (string.Equals(typeName, "TypeCollectionBase", StringComparison.Ordinal) ||
                string.Equals(typeName, "ServiceTypeCollectionBase", StringComparison.Ordinal))
            {
                // First type argument is TBase (e.g., DataStoreTypeBase<...>)
                if (currentType.TypeArguments.Length > 0 &&
                    currentType.TypeArguments[0] is INamedTypeSymbol baseType)
                {
                    // Check the base of TBase to find TypeOptionBase<TId, TSelf>
                    var optionBase = baseType.BaseType;
                    while (optionBase != null)
                    {
                        if (string.Equals(optionBase.Name, "TypeOptionBase", StringComparison.Ordinal) &&
                            optionBase.TypeArguments.Length > 0)
                        {
                            var idType = optionBase.TypeArguments[0];
                            return string.Equals(idType.Name, "Guid", StringComparison.Ordinal) ||
                                   string.Equals(idType.ToDisplayString(), "System.Guid", StringComparison.Ordinal);
                        }
                        optionBase = optionBase.BaseType;
                    }
                }
                break;
            }
            currentType = currentType.BaseType;
        }
        return false;
    }

    private static ImmutableArray<ConstructorInfo> ExtractConstructors(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.InstanceConstructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public)
            .Where(c => !c.IsImplicitlyDeclared)
            .Select(c => new ConstructorInfo(
                Parameters: c.Parameters
                    .Select(p => new ParameterInfo(
                        Name: p.Name,
                        Type: p.Type.ToDisplayString(),
                        HasDefaultValue: p.HasExplicitDefaultValue,
                        DefaultValue: FormatDefaultValue(p)
                    ))
                    .ToImmutableArray()
            ))
            .ToImmutableArray();
    }

    [ConventionOverride(MaxCyclomaticComplexity = 16)]  // Exhaustive type mapping for C# literal formatting
    private static string FormatDefaultValue(IParameterSymbol parameter)
    {
        if (!parameter.HasExplicitDefaultValue)
            return "";

        var value = parameter.ExplicitDefaultValue;

        return value switch
        {
            null => "default!",
            string s => $"\"{EscapeString(s)}\"",
            bool b => b ? "true" : "false",
            char c => $"'{c}'",
            float f => $"{f}f",
            double d => $"{d}d",
            decimal m => $"{m}m",
            _ => value.ToString() ?? "default!"
        };
    }

    private static string EscapeString(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    /// <summary>
    /// Builds the name <see cref="System.Type.FullName"/> would report for this symbol.
    /// </summary>
    /// <param name="symbol">The option type.</param>
    /// <returns>Namespace-qualified name, nested types joined with '+'.</returns>
    /// <remarks>
    /// Not ToDisplayString: the fully-qualified format prefixes "global::" and joins nested types
    /// with '.', where the runtime uses '+'. Either difference changes the hash and breaks the
    /// agreement this exists to keep. Generic types are already rejected before this is reached,
    /// so no arity suffix or type-argument list has to be reproduced.
    /// </remarks>
    private static string GetClrFullName(INamedTypeSymbol symbol)
    {
        var name = symbol.MetadataName;

        for (var outer = symbol.ContainingType; outer != null; outer = outer.ContainingType)
        {
            name = outer.MetadataName + "+" + name;
        }

        var ns = symbol.ContainingNamespace;
        return ns == null || ns.IsGlobalNamespace ? name : ns.ToDisplayString() + "." + name;
    }

    private static int GenerateIdFromName(string name)
    {
        unchecked
        {
            const int FnvPrime = 0x01000193;
            const int FnvOffsetBasis = (int)0x811C9DC5;

            int hash = FnvOffsetBasis;
            foreach (char c in name)
            {
                hash ^= c;
                hash *= FnvPrime;
            }
            return hash & 0x7FFFFFFF;
        }
    }

    private static void Execute(
        SourceProductionContext context,
        ImmutableArray<ExtensionOptionModel> options)
    {
        if (options.Length == 0)
            return;

        foreach (var option in options)
        {
            var code = GenerateExtension(option);
            var fileName = $"{option.CollectionClassName}.{option.OptionName}Extension.g.cs";
            context.AddSource(fileName, SourceText.From(code, Encoding.UTF8));
        }
    }

#pragma warning disable MA0051 // Source generator emits complete extension class — splitting scatters the template
    private static string GenerateExtension(ExtensionOptionModel option)
    {
        var sb = new StringBuilder();

        // Determine the return type
        var returnType = option.MethodReturnTypeName ?? option.FullTypeName;

        // Is it a singleton or factory collection?
        var isSingleton = option.CollectionKind != CollectionKind.Factory;

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine($"namespace {option.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// Provides static extension methods for <see cref=\"{option.TypeName}\"/> on <see cref=\"{option.CollectionFullName}\"/>.");
        sb.AppendLine($"/// </summary>");
        sb.AppendLine("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
        sb.AppendLine($"public static class {option.CollectionClassName}_{option.OptionName}Extension");
        sb.AppendLine("{");
        sb.AppendLine($"    extension({option.CollectionFullName})");
        sb.AppendLine("    {");

        // Find parameterless constructor
        var hasParameterlessConstructor = option.Constructors.Any(c => c.Parameters.Length == 0);

        // Default accessor (parameterless)
        if (isSingleton && hasParameterlessConstructor)
        {
            // Singleton: return from collection
            sb.AppendLine($"        /// <summary>Gets the {option.OptionName} singleton from the collection.</summary>");
            sb.AppendLine($"        /// <remarks>Auto-generated by <c>TypeOptionExtensionGenerator</c>. To override, define a static method");
            sb.AppendLine($"        /// with this exact signature in the partial class — the generator will detect it and skip generation.</remarks>");
            sb.AppendLine($"        public static {returnType} {option.OptionName}() =>");
            if (option.IsGuidBasedCollection)
            {
                // ServiceTypeCollections use Guid-based ById, so we use ByName for those
                sb.AppendLine($"            ({returnType}){option.CollectionFullName}.ByName(\"{option.OptionName}\");");
            }
            else
            {
                sb.AppendLine($"            ({returnType}){option.CollectionFullName}.ById({option.GeneratedId});");
            }
        }
        else if (hasParameterlessConstructor)
        {
            // Factory: return new instance
            sb.AppendLine($"        /// <summary>Creates a new instance of {option.TypeName}.</summary>");
            sb.AppendLine($"        /// <remarks>Auto-generated by <c>TypeOptionExtensionGenerator</c>. To override, define a static method");
            sb.AppendLine($"        /// with this exact signature in the partial class — the generator will detect it and skip generation.</remarks>");
            sb.AppendLine($"        public static {returnType} {option.OptionName}() =>");
            sb.AppendLine($"            new {option.FullTypeName}();");
        }

        // Parameterized overloads - always create new instances
        foreach (var ctor in option.Constructors.Where(c => c.Parameters.Length > 0))
        {
            sb.AppendLine();

            var parameters = string.Join(", ",
                ctor.Parameters.Select(p =>
                    p.HasDefaultValue
                        ? $"{p.Type} {p.Name} = {p.DefaultValue}"
                        : $"{p.Type} {p.Name}"));

            var arguments = string.Join(", ", ctor.Parameters.Select(p => p.Name));

            sb.AppendLine($"        /// <summary>Creates a new instance of {option.TypeName} with parameters.</summary>");
            sb.AppendLine($"        /// <remarks>Auto-generated by <c>TypeOptionExtensionGenerator</c>. To override, define a static method");
            sb.AppendLine($"        /// with this exact signature in the partial class — the generator will detect it and skip generation.</remarks>");
            sb.AppendLine($"        public static {returnType} {option.OptionName}({parameters}) =>");
            sb.AppendLine($"            new {option.FullTypeName}({arguments});");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    // Local enums and models for this generator
    // Pragma required: source generators cannot use TypeCollections (bootstrapping problem)
#pragma warning disable FDW017
    private enum CollectionKind
    {
        Singleton,
        Mutable,
        Factory
    }
#pragma warning restore FDW017

    private readonly record struct ExtensionOptionModel(
        string TypeName,
        string FullTypeName,
        string Namespace,
        string CollectionFullName,
        string CollectionClassName,
        string OptionName,
        int GeneratedId,
        ImmutableArray<ConstructorInfo> Constructors,
        string? MethodReturnTypeName,
        CollectionKind CollectionKind,
        bool IsServiceType,
        bool IsGuidBasedCollection  // ServiceTypeCollections use Guid IDs
    );

    private readonly record struct ConstructorInfo(
        ImmutableArray<ParameterInfo> Parameters
    );

    private readonly record struct ParameterInfo(
        string Name,
        string Type,
        bool HasDefaultValue,
        string DefaultValue
    );
}
