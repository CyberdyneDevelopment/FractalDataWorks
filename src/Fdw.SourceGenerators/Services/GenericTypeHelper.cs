using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Fdw.SourceGenerators.Services;

/// <summary>
/// Helper utilities for working with generic types in source generators.
/// Handles type arity detection, metadata format conversion, and generic type parameter extraction.
/// </summary>
public static class GenericTypeHelper
{
    /// <summary>
    /// Checks if a type is generic (has type parameters).
    /// </summary>
    public static bool IsGenericType(INamedTypeSymbol? typeSymbol)
    {
        return typeSymbol?.IsGenericType == true && typeSymbol.TypeParameters.Length > 0;
    }

    /// <summary>
    /// Gets the arity (number of type parameters) for a type.
    /// Returns 0 for non-generic types.
    /// </summary>
    public static int GetArity(INamedTypeSymbol? typeSymbol)
    {
        if (typeSymbol == null) return 0;
        return typeSymbol.IsGenericType ? typeSymbol.TypeParameters.Length : 0;
    }

    /// <summary>
    /// Gets the metadata name for a type (e.g., "Type`2" for Type&lt;T1,T2&gt;).
    /// This is the format required by Compilation.GetTypeByMetadataName().
    /// </summary>
    public static string GetMetadataName(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol == null)
            throw new ArgumentNullException(nameof(typeSymbol));

        var arity = GetArity(typeSymbol);
        var name = typeSymbol.Name;

        // For generic types, append backtick and arity
        if (arity > 0)
        {
            return $"{name}`{arity}";
        }

        return name;
    }

    /// <summary>
    /// Gets the fully qualified metadata name including namespace (e.g., "Namespace.Type`2").
    /// </summary>
    public static string GetFullMetadataName(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol == null)
            throw new ArgumentNullException(nameof(typeSymbol));

        var metadataName = GetMetadataName(typeSymbol);
        var namespaceStr = typeSymbol.ContainingNamespace?.ToDisplayString();

        if (!string.IsNullOrEmpty(namespaceStr))
        {
            return $"{namespaceStr}.{metadataName}";
        }

        return metadataName;
    }

    /// <summary>
    /// Gets the type parameter list as a string (e.g., "&lt;T1, T2, T3&gt;").
    /// Returns empty string for non-generic types.
    /// </summary>
    public static string GetTypeParameterList(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol == null || !IsGenericType(typeSymbol))
            return string.Empty;

        var parameters = typeSymbol.TypeParameters.Select(p => p.Name);
        return $"<{string.Join(", ", parameters)}>";
    }

    /// <summary>
    /// Gets the type parameter list with constraints as a string for class declarations.
    /// Example: "where T1 : class where T2 : struct"
    /// </summary>
    public static string GetTypeParameterConstraints(INamedTypeSymbol typeSymbol, string indent = "    ")
    {
        if (typeSymbol == null || !IsGenericType(typeSymbol))
            return string.Empty;

        var constraints = new List<string>();

        foreach (var typeParam in typeSymbol.TypeParameters)
        {
            var constraintParts = new List<string>();

            // Reference type constraint
            if (typeParam.HasReferenceTypeConstraint)
            {
                constraintParts.Add("class");
            }

            // Value type constraint
            if (typeParam.HasValueTypeConstraint)
            {
                constraintParts.Add("struct");
            }

            // Constructor constraint
            if (typeParam.HasConstructorConstraint)
            {
                constraintParts.Add("new()");
            }

            // Type constraints
            foreach (var constraintType in typeParam.ConstraintTypes)
            {
                constraintParts.Add(constraintType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }

            if (constraintParts.Count > 0)
            {
                constraints.Add($"{indent}where {typeParam.Name} : {string.Join(", ", constraintParts)}");
            }
        }

        return constraints.Count > 0 ? "\n" + string.Join("\n", constraints) : string.Empty;
    }

    /// <summary>
    /// Extracts the simple type name without generic arity suffix or type parameters.
    /// E.g., "ConnectionTypeBase&lt;T1,T2,T3&gt;" => "ConnectionTypeBase"
    /// </summary>
    public static string GetSimpleTypeName(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol == null)
            throw new ArgumentNullException(nameof(typeSymbol));

        return typeSymbol.Name;
    }

    /// <summary>
    /// Gets the simple type name with arity for display purposes.
    /// E.g., "ConnectionTypeBase&lt;T1,T2,T3&gt;" => "ConnectionTypeBase"
    /// This is the name without generic type arguments.
    /// </summary>
    public static string GetSimpleTypeNameWithParameters(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol == null)
            throw new ArgumentNullException(nameof(typeSymbol));

        var name = typeSymbol.Name;
        var parameters = GetTypeParameterList(typeSymbol);
        return $"{name}{parameters}";
    }

    /// <summary>
    /// Gets the fully qualified type name with type parameters for code generation.
    /// E.g., "Namespace.ConnectionTypeBase&lt;TService, TConfiguration, TFactory&gt;"
    /// </summary>
    public static string GetFullyQualifiedNameWithParameters(INamedTypeSymbol typeSymbol)
    {
        if (typeSymbol == null)
            throw new ArgumentNullException(nameof(typeSymbol));

        var ns = typeSymbol.ContainingNamespace?.ToDisplayString();
        var nameWithParams = GetSimpleTypeNameWithParameters(typeSymbol);

        if (!string.IsNullOrEmpty(ns))
        {
            return $"{ns}.{nameWithParams}";
        }

        return nameWithParams;
    }

    /// <summary>
    /// Resolves a type symbol by metadata name, handling both generic and non-generic types.
    /// </summary>
    public static INamedTypeSymbol? ResolveType(INamedTypeSymbol typeSymbol, Compilation compilation)
    {
        if (typeSymbol == null || compilation == null)
            return null;

        // For generic types, use the original definition
        if (IsGenericType(typeSymbol))
        {
            var metadataName = GetFullMetadataName(typeSymbol.OriginalDefinition);
            return compilation.GetTypeByMetadataName(metadataName);
        }

        // For non-generic types, use standard metadata name
        var fullName = typeSymbol.ToDisplayString();
        return compilation.GetTypeByMetadataName(fullName);
    }

    /// <summary>
    /// Creates a generic type declaration string for generated code.
    /// E.g., "class EmptyConnectionTypeBase&lt;TService, TConfiguration, TFactory&gt; : ConnectionTypeBase&lt;TService, TConfiguration, TFactory&gt;"
    /// </summary>
    public static string CreateGenericClassDeclaration(
        string className,
        INamedTypeSymbol baseType,
        string accessModifier = "public",
        bool isSealed = false)
    {
        var sb = new StringBuilder();

        sb.Append(accessModifier);
        if (isSealed)
            sb.Append(" sealed");

        sb.Append($" class {className}");

        // Add type parameters if base is generic
        var typeParams = GetTypeParameterList(baseType);
        if (!string.IsNullOrEmpty(typeParams))
        {
            sb.Append(typeParams);
        }

        sb.Append($" : {GetSimpleTypeNameWithParameters(baseType)}");

        return sb.ToString();
    }
}
