using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.Collections.SourceGenerators.Shared;

/// <summary>
/// Shared logic for discovering ServiceTypeOptions across assemblies.
/// </summary>
internal static class ServiceTypeOptionDiscovery
{
    private const string ServiceTypeOptionAttributeName = "Fdw.Collections.ServiceTypeOptionAttribute";
    private const string TypeLookupAttributeName = "Fdw.Collections.Attributes.TypeLookupAttribute";

    /// <summary>
    /// Discovers all ServiceTypeOptions in the compilation and optionally referenced assemblies.
    /// </summary>
    public static ImmutableArray<ServiceTypeOptionModel> DiscoverAll(
        Compilation compilation,
        bool restrictToCurrentCompilation)
    {
        var results = new List<ServiceTypeOptionModel>();

        var optionAttrType = compilation.GetTypeByMetadataName(ServiceTypeOptionAttributeName);
        var lookupAttrType = compilation.GetTypeByMetadataName(TypeLookupAttributeName);

        if (optionAttrType == null)
            return ImmutableArray<ServiceTypeOptionModel>.Empty;

        // Always scan current assembly
        var visitor = new ServiceTypeOptionVisitor(optionAttrType, lookupAttrType, results);
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
    /// Generates a deterministic Guid from a type name using UUID v5 algorithm.
    /// </summary>
    [SuppressMessage("Security", "CA5350:Do not use weak cryptographic algorithms", Justification = "SHA1 is required by UUID v5 spec (RFC 4122), not used for security.")]
    [SuppressMessage("Security", "SCS0006:Weak hashing function", Justification = "SHA1 is required by UUID v5 spec (RFC 4122), not used for security.")]
    public static Guid GenerateGuidFromTypeName(string fullTypeName)
    {
        // Using DNS namespace UUID as base
        var namespaceId = new Guid("6ba7b810-9dad-11d1-80b4-00c04fd430c8");
        var nameBytes = Encoding.UTF8.GetBytes("Fdw.Collections." + fullTypeName);

        byte[] hash;
        using (var sha1 = SHA1.Create())
        {
            var namespaceBytes = namespaceId.ToByteArray();
            SwapGuidBytes(namespaceBytes);

            sha1.TransformBlock(namespaceBytes, 0, namespaceBytes.Length, null, 0);
            sha1.TransformFinalBlock(nameBytes, 0, nameBytes.Length);
            hash = sha1.Hash!;
        }

        var guidBytes = new byte[16];
        Array.Copy(hash, 0, guidBytes, 0, 16);

        // Set version to 5 (SHA-1 based)
        guidBytes[6] = (byte)((guidBytes[6] & 0x0F) | 0x50);
        // Set variant to RFC 4122
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

        SwapGuidBytes(guidBytes);
        return new Guid(guidBytes);
    }

    private static void SwapGuidBytes(byte[] bytes)
    {
        (bytes[0], bytes[3]) = (bytes[3], bytes[0]);
        (bytes[1], bytes[2]) = (bytes[2], bytes[1]);
        (bytes[4], bytes[5]) = (bytes[5], bytes[4]);
        (bytes[6], bytes[7]) = (bytes[7], bytes[6]);
    }

    /// <summary>
    /// Computes a deterministic Guid for a ServiceTypeOption by matching the runtime
    /// ServiceTypeBase&lt;TService, TFactory&gt;.Id computation (MD5 of TService+TFactory full names).
    /// Falls back to UUID v5 of concrete type name for types not inheriting from ServiceTypeBase.
    /// </summary>
    [SuppressMessage("Security", "CA5351:Do not use weak cryptographic algorithms", Justification = "MD5 used for deterministic ID generation matching runtime ServiceTypeBase, not for security.")]
    [SuppressMessage("Security", "SCS0006:Weak hashing function", Justification = "MD5 used for deterministic ID generation matching runtime ServiceTypeBase, not for security.")]
    [SuppressMessage("Security", "CA1850:Prefer static HashData method", Justification = "netstandard2.0 target does not have static HashData.")]
    public static Guid ComputeServiceTypeId(INamedTypeSymbol typeSymbol)
    {
        // Walk up the inheritance chain to find ServiceTypeBase<TService, TFactory, ...>
        var current = typeSymbol.BaseType;
        while (current != null)
        {
            var original = current.OriginalDefinition;
            if (string.Equals(original.Name, "ServiceTypeBase", StringComparison.Ordinal) &&
                string.Equals(original.ContainingNamespace.ToDisplayString(), "Fdw.ServiceTypes", StringComparison.Ordinal) &&
                current.TypeArguments.Length >= 2)
            {
                var tService = current.TypeArguments[0];
                var tFactory = current.TypeArguments[1];
                var input = $"{tService.ToDisplayString()}:{tFactory.ToDisplayString()}";

                using (var md5 = MD5.Create())
                {
                    var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                    return new Guid(hash);
                }
            }

            current = current.BaseType;
        }

        // Fallback for types not inheriting from ServiceTypeBase
        return GenerateGuidFromTypeName(typeSymbol.ToDisplayString());
    }

    private sealed class ServiceTypeOptionVisitor : SymbolVisitor
    {
        private readonly INamedTypeSymbol _optionAttrType;
        private readonly INamedTypeSymbol? _lookupAttrType;
        private readonly List<ServiceTypeOptionModel> _results;

        public ServiceTypeOptionVisitor(
            INamedTypeSymbol optionAttrType,
            INamedTypeSymbol? lookupAttrType,
            List<ServiceTypeOptionModel> results)
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
                var model = ExtractServiceTypeOptionModel(symbol, attr);
                if (model != null)
                    _results.Add(model.Value);
            }

            foreach (var nested in symbol.GetTypeMembers())
                nested.Accept(this);
        }

        private ServiceTypeOptionModel? ExtractServiceTypeOptionModel(
            INamedTypeSymbol typeSymbol,
            AttributeData attribute)
        {
            if (attribute.ConstructorArguments.Length < 2)
                return null;

            var collectionType = attribute.ConstructorArguments[0].Value as ITypeSymbol;
            var optionName = attribute.ConstructorArguments[1].Value?.ToString() ?? "";

            if (collectionType == null || string.IsNullOrEmpty(optionName))
                return null;

            var collectionMatchKey = GetMatchKey(collectionType);
            var generatedId = ComputeServiceTypeId(typeSymbol);
            var constructors = ExtractConstructors(typeSymbol);
            var lookupProperties = DiscoverLookupProperties(typeSymbol, _lookupAttrType);

            return new ServiceTypeOptionModel(
                TypeName: typeSymbol.Name,
                FullTypeName: typeSymbol.ToDisplayString(),
                Namespace: typeSymbol.ContainingNamespace.ToDisplayString(),
                CollectionMatchKey: collectionMatchKey,
                OptionName: optionName,
                GeneratedId: generatedId,
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

                            // Why index 2 with an absent-means-true guard: isUnique is the third constructor
                            // parameter and is optional. Roslyn supplies defaults for omitted optional
                            // arguments, but an attribute compiled against an older version of this type
                            // supplies fewer, and reading a missing argument as false would silently turn
                            // an enforced uniqueness promise into a list nobody checks.
                            var isUnique = attr.ConstructorArguments.Length <= 2
                                || attr.ConstructorArguments[2].Value is not bool u || u;

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
                string? extractedValue = null;
                var paramName = ToCamelCase(def.PropertyName);
                if (extractedValues.TryGetValue(paramName, out var value))
                {
                    extractedValue = value;
                }
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

        private static Dictionary<string, string> ExtractBaseConstructorArguments(INamedTypeSymbol typeSymbol)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var parameterlessCtor = typeSymbol.InstanceConstructors
                .FirstOrDefault(c => c.Parameters.Length == 0 && !c.IsImplicitlyDeclared);

            if (parameterlessCtor == null)
                return result;

            var syntaxRef = parameterlessCtor.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntaxRef == null)
                return result;

            var syntax = syntaxRef.GetSyntax();
            if (syntax is not ConstructorDeclarationSyntax ctorSyntax)
                return result;

            var initializer = ctorSyntax.Initializer;
            if (initializer == null || !initializer.ThisOrBaseKeyword.IsKind(SyntaxKind.BaseKeyword))
                return result;

            var baseType = typeSymbol.BaseType;
            if (baseType == null)
                return result;

            var argCount = initializer.ArgumentList.Arguments.Count;
            var baseCtors = baseType.InstanceConstructors
                .Where(c => c.Parameters.Length == argCount)
                .ToList();

            if (baseCtors.Count != 1)
                return result;

            var baseCtor = baseCtors[0];

            for (int i = 0; i < argCount; i++)
            {
                var arg = initializer.ArgumentList.Arguments[i];
                var param = baseCtor.Parameters[i];

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
                LiteralExpressionSyntax { RawKind: var kind } when kind == (int)SyntaxKind.DefaultLiteralExpression => "default",
                LiteralExpressionSyntax { RawKind: var kind } when kind == (int)SyntaxKind.NullLiteralExpression => "null",

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