using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Fdw.Conventions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Collections.SourceGenerators.Shared;

/// <summary>
/// Shared logic for discovering TypeOptions across assemblies.
/// </summary>
internal static class TypeOptionDiscovery
{
    private const string TypeOptionAttributeName = "Fdw.Collections.Attributes.TypeOptionAttribute";
    private const string TypeLookupAttributeName = "Fdw.Collections.Attributes.TypeLookupAttribute";

    /// <summary>
    /// Discovers all TypeOptions in the compilation and optionally referenced assemblies.
    /// </summary>
    public static ImmutableArray<TypeOptionModel> DiscoverAll(
        Compilation compilation,
        bool restrictToCurrentCompilation)
    {
        var results = new List<TypeOptionModel>();

        var optionAttrType = compilation.GetTypeByMetadataName(TypeOptionAttributeName);
        var lookupAttrType = compilation.GetTypeByMetadataName(TypeLookupAttributeName);

        if (optionAttrType == null)
            return ImmutableArray<TypeOptionModel>.Empty;

        // Always scan current assembly
        var visitor = new TypeOptionVisitor(optionAttrType, lookupAttrType, results);
        visitor.Visit(compilation.Assembly.GlobalNamespace);

        // Scan referenced assemblies unless restricted
        if (!restrictToCurrentCompilation)
        {
            foreach (var assembly in compilation.SourceModule.ReferencedAssemblySymbols)
            {
                visitor.Visit(assembly.GlobalNamespace);
            }
        }

        return results.ToImmutableArray();
    }

    /// <summary>
    /// Gets the match key for a type (handles unbound generics).
    /// </summary>
    public static string GetMatchKey(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named)
        {
            if (named.IsUnboundGenericType || named.IsGenericType)
                return named.OriginalDefinition.ToDisplayString();
        }
        return type.ToDisplayString();
    }

    /// <summary>
    /// Generates a stable ID from a type name using FNV-1a hash.
    /// </summary>
    public static int GenerateIdFromTypeName(string fullTypeName)
    {
        unchecked
        {
            const int FnvPrime = 0x01000193;
            const int FnvOffsetBasis = (int)0x811C9DC5;

            int hash = FnvOffsetBasis;
            foreach (char c in fullTypeName)
            {
                hash ^= c;
                hash *= FnvPrime;
            }
            return hash & 0x7FFFFFFF;
        }
    }

    private sealed class TypeOptionVisitor : SymbolVisitor
    {
        private readonly INamedTypeSymbol _optionAttrType;
        private readonly INamedTypeSymbol? _lookupAttrType;
        private readonly List<TypeOptionModel> _results;

        public TypeOptionVisitor(
            INamedTypeSymbol optionAttrType,
            INamedTypeSymbol? lookupAttrType,
            List<TypeOptionModel> results)
        {
            _optionAttrType = optionAttrType;
            _lookupAttrType = lookupAttrType;
            _results = results;
        }

        public override void VisitNamespace(INamespaceSymbol symbol)
        {
            foreach (var member in symbol.GetMembers())
                member.Accept(this);
        }

        public override void VisitNamedType(INamedTypeSymbol symbol)
        {
            var attr = symbol.GetAttributes()
                .FirstOrDefault(a => SymbolEqualityComparer.Default.Equals(
                    a.AttributeClass, _optionAttrType));

            if (attr != null)
            {
                var model = ExtractTypeOptionModel(symbol, attr);
                if (model != null)
                    _results.Add(model.Value);
            }

            foreach (var nested in symbol.GetTypeMembers())
                nested.Accept(this);
        }

        private TypeOptionModel? ExtractTypeOptionModel(
            INamedTypeSymbol typeSymbol,
            AttributeData attribute)
        {
            if (attribute.ConstructorArguments.Length < 2)
                return null;

            var collectionType = attribute.ConstructorArguments[0].Value as ITypeSymbol;
            var optionName = attribute.ConstructorArguments[1].Value?.ToString() ?? "";

            if (collectionType == null || string.IsNullOrEmpty(optionName))
                return null;

            // Extract Category from named arguments
            string? category = null;
            foreach (var namedArg in attribute.NamedArguments)
            {
                if (string.Equals(namedArg.Key, "Category", StringComparison.Ordinal) &&
                    namedArg.Value.Value is string categoryValue)
                {
                    category = categoryValue;
                    break;
                }
            }

            var collectionMatchKey = GetMatchKey(collectionType);
            var generatedId = GenerateIdFromTypeName(typeSymbol.ToDisplayString());
            var constructors = ExtractConstructors(typeSymbol);
            var lookupProperties = DiscoverLookupProperties(typeSymbol, _lookupAttrType);

            return new TypeOptionModel(
                TypeName: typeSymbol.Name,
                FullTypeName: typeSymbol.ToDisplayString(),
                Namespace: typeSymbol.ContainingNamespace.ToDisplayString(),
                CollectionMatchKey: collectionMatchKey,
                OptionName: optionName,
                GeneratedId: generatedId,
                Category: category,
                Constructors: constructors,
                LookupProperties: lookupProperties
            );
        }

        private static ImmutableArray<ConstructorModel> ExtractConstructors(
            INamedTypeSymbol typeSymbol)
        {
            return typeSymbol.InstanceConstructors
                .Where(c => c.DeclaredAccessibility == Accessibility.Public)
                .Where(c => !c.IsImplicitlyDeclared)
                .Select(c => new ConstructorModel(
                    Parameters: c.Parameters
                        .Select(p => new ParameterModel(
                            Name: p.Name,
                            Type: p.Type.ToDisplayString(),
                            HasDefaultValue: p.HasExplicitDefaultValue,
                            DefaultValue: FormatDefaultValue(p)
                        ))
                        .ToImmutableArray()
                ))
                .ToImmutableArray();
        }

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

#pragma warning disable MA0051 // Roslyn symbol inspection walks inheritance chain — must stay cohesive
        private static ImmutableArray<LookupPropertyModel> DiscoverLookupProperties(
            INamedTypeSymbol typeSymbol,
            INamedTypeSymbol? lookupAttrType)
        {
            if (lookupAttrType == null)
                return ImmutableArray<LookupPropertyModel>.Empty;

            // First, discover all lookup property definitions from the inheritance chain
            var propertyDefs = new List<(string PropertyName, string PropertyType, string MethodName, bool IsUnique)>();
            var currentType = typeSymbol;

            while (currentType != null)
            {
                foreach (var property in currentType.GetMembers().OfType<IPropertySymbol>())
                {
                    var lookupAttrs = property.GetAttributes()
                        .Where(a => SymbolEqualityComparer.Default.Equals(
                            a.AttributeClass, lookupAttrType));

                    foreach (var attr in lookupAttrs)
                    {
                        if (attr.ConstructorArguments.Length > 0)
                        {
                            var methodName = attr.ConstructorArguments[0].Value?.ToString() ?? "";

                            var isUnique = attr.ConstructorArguments.Length <= 2
                                || attr.ConstructorArguments[2].Value is not bool u || u;

                            // Avoid duplicates from overridden properties
                            if (!propertyDefs.Any(r => string.Equals(r.PropertyName, property.Name, StringComparison.Ordinal) &&
                                                       string.Equals(r.MethodName, methodName, StringComparison.Ordinal)))
                            {
                                propertyDefs.Add((property.Name, property.Type.ToDisplayString(), methodName, isUnique));
                            }
                        }
                    }
                }

                currentType = currentType.BaseType;
            }

            // Now try to extract values from the TypeOption's constructor
            var extractedValues = ExtractBaseConstructorArguments(typeSymbol);

            // Build the result with extracted values where possible
            var results = new List<LookupPropertyModel>();
            foreach (var def in propertyDefs)
            {
                // Try to find the value for this property
                // We look for a constructor parameter with a matching name (case-insensitive)
                string? extractedValue = null;
                var paramName = ToCamelCase(def.PropertyName);
                if (extractedValues.TryGetValue(paramName, out var value))
                {
                    extractedValue = value;
                }
                // Also try exact match
                else if (extractedValues.TryGetValue(def.PropertyName, out value))
                {
                    extractedValue = value;
                }

                results.Add(new LookupPropertyModel(
                    PropertyName: def.PropertyName,
                    PropertyType: def.PropertyType,
                    MethodName: def.MethodName,
                    ExtractedValue: extractedValue,
                    IsUnique: def.IsUnique
                ));
            }

            return results.ToImmutableArray();
        }

        /// <summary>
        /// Extracts literal argument values from the base() initializer of a TypeOption's parameterless constructor.
        /// Returns a dictionary mapping parameter names to their string representations.
        /// </summary>
        private static Dictionary<string, string> ExtractBaseConstructorArguments(INamedTypeSymbol typeSymbol)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Find the parameterless constructor
            var parameterlessCtor = typeSymbol.InstanceConstructors
                .FirstOrDefault(c => c.Parameters.Length == 0 && !c.IsImplicitlyDeclared);

            if (parameterlessCtor == null)
                return result;

            // Get the syntax for the constructor
            var syntaxRef = parameterlessCtor.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxRef == null)
                return result;

            var syntax = syntaxRef.GetSyntax();
            if (syntax is not ConstructorDeclarationSyntax ctorSyntax)
                return result;

            // Get the base() initializer
            var initializer = ctorSyntax.Initializer;
            if (initializer == null || !initializer.ThisOrBaseKeyword.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.BaseKeyword))
                return result;

            // Get the base class constructor parameters
            var baseType = typeSymbol.BaseType;
            if (baseType == null)
                return result;

            // Find a matching constructor in base class by argument count
            var argCount = initializer.ArgumentList.Arguments.Count;
            var baseCtors = baseType.InstanceConstructors
                .Where(c => c.Parameters.Length == argCount)
                .ToList();

            if (baseCtors.Count != 1)
                return result; // Ambiguous or no match

            var baseCtor = baseCtors[0];

            // Map arguments to parameters
            for (int i = 0; i < argCount; i++)
            {
                var arg = initializer.ArgumentList.Arguments[i];
                var param = baseCtor.Parameters[i];

                // Try to extract the literal value
                var literalValue = ExtractLiteralValue(arg.Expression);
                if (literalValue != null)
                {
                    result[param.Name] = literalValue;
                }
            }

            return result;
        }

        /// <summary>
        /// Extracts a string representation from an expression.
        /// Handles literals, typeof(), nameof(), and other common patterns.
        /// Returns null if the expression cannot be statically evaluated.
        /// </summary>
        private static string? ExtractLiteralValue(ExpressionSyntax expression)
        {
            return expression switch
            {
                // default or default(T) - must come before general LiteralExpressionSyntax
                DefaultExpressionSyntax => "default",

                // Specific literal kinds - must come before general LiteralExpressionSyntax
                LiteralExpressionSyntax { RawKind: var kind } when kind == (int)Microsoft.CodeAnalysis.CSharp.SyntaxKind.DefaultLiteralExpression => "default",
                LiteralExpressionSyntax { RawKind: var kind } when kind == (int)Microsoft.CodeAnalysis.CSharp.SyntaxKind.NullLiteralExpression => "null",

                // Simple literals: "string", 123, true, etc.
                LiteralExpressionSyntax literal => literal.Token.ValueText,

                // Negative numbers: -5
                PrefixUnaryExpressionSyntax prefix when prefix.Operand is LiteralExpressionSyntax lit =>
                    prefix.OperatorToken.Text + lit.Token.ValueText,

                // typeof(SomeType) or typeof(SomeType<T>)
                TypeOfExpressionSyntax typeOf => typeOf.Type.ToString(),

                // nameof(something)
                InvocationExpressionSyntax invocation
                    when invocation.Expression is IdentifierNameSyntax id
                    && string.Equals(id.Identifier.ValueText, "nameof", StringComparison.Ordinal)
                    && invocation.ArgumentList.Arguments.Count == 1 =>
                    invocation.ArgumentList.Arguments[0].Expression.ToString(),

                _ => null
            };
        }

        private static string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }
    }
}
