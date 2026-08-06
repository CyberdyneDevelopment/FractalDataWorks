using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fdw.CodeBuilder.Analysis.CSharp.Compilations;

/// <summary>
/// Provides semantic type comparison for testing generated code.
/// </summary>
/// <remarks>
/// This code is excluded from code coverage because it is test infrastructure code that supports testing but is not production code.
/// </remarks>
[ExcludeFromCodeCoverage]
internal static class TypeComparer
{
    /// <summary>
    /// Determines if two type strings are semantically equivalent.
    /// </summary>
    /// <param name="actualTypeSyntax">The actual type syntax from generated code.</param>
    /// <param name="expectedType">The expected type string.</param>
    /// <returns>True if the types are semantically equivalent; otherwise, false.</returns>
    public static bool AreEquivalent(TypeSyntax actualTypeSyntax, string expectedType)
    {
        if (actualTypeSyntax == null || string.IsNullOrWhiteSpace(expectedType))
            return false;

        // Parse the expected type string into syntax
        var expectedTypeSyntax = SyntaxFactory.ParseTypeName(expectedType);

        return AreEquivalent(actualTypeSyntax, expectedTypeSyntax);
    }

    /// <summary>
    /// Determines if two type syntax nodes are semantically equivalent.
    /// </summary>
    private static bool AreEquivalent(TypeSyntax actual, TypeSyntax expected)
    {
        return (actual, expected) switch
        {
            // Handle nullable types (e.g., CountryBase? vs TestNamespace.CountryBase?)
            (NullableTypeSyntax actualNullable, NullableTypeSyntax expectedNullable) =>
                AreEquivalent(actualNullable.ElementType, expectedNullable.ElementType),

            // Handle nullable where only one side has the ? annotation
            (NullableTypeSyntax actualNullable, _) =>
                AreEquivalent(actualNullable.ElementType, expected),

            (_, NullableTypeSyntax expectedNullable) =>
                AreEquivalent(actual, expectedNullable.ElementType),

            // Handle generic types (e.g., List<T>, IEnumerable<T>)
            (GenericNameSyntax actualGeneric, GenericNameSyntax expectedGeneric) =>
                AreGenericTypesEquivalent(actualGeneric, expectedGeneric),

            // Handle qualified names (e.g., System.String vs String)
            (QualifiedNameSyntax actualQualified, _) =>
                AreEquivalent(actualQualified.Right, expected),

            (_, QualifiedNameSyntax expectedQualified) =>
                AreEquivalent(actual, expectedQualified.Right),

            // Handle array types
            (ArrayTypeSyntax actualArray, ArrayTypeSyntax expectedArray) =>
                AreEquivalent(actualArray.ElementType, expectedArray.ElementType) &&
                actualArray.RankSpecifiers.Count == expectedArray.RankSpecifiers.Count,

            // Handle simple identifiers
            (IdentifierNameSyntax actualId, IdentifierNameSyntax expectedId) =>
                AreIdentifiersEquivalent(actualId.Identifier.Text, expectedId.Identifier.Text),

            // Handle predefined types (int, string, etc.)
            (PredefinedTypeSyntax actualPredefined, PredefinedTypeSyntax expectedPredefined) =>
                AreKeywordsEquivalent(actualPredefined.Keyword, expectedPredefined.Keyword),

            // Handle predefined type vs identifier (e.g., string vs String)
            (PredefinedTypeSyntax actualPredefined, IdentifierNameSyntax expectedId) =>
                IsPredefinedTypeEquivalent(actualPredefined, expectedId.Identifier.Text),

            (IdentifierNameSyntax actualId, PredefinedTypeSyntax expectedPredefined) =>
                IsPredefinedTypeEquivalent(expectedPredefined, actualId.Identifier.Text),

            // Default case
            _ => false
        };
    }

    private static bool AreGenericTypesEquivalent(GenericNameSyntax actual, GenericNameSyntax expected)
    {
        // Check if base type names match
        if (!AreIdentifiersEquivalent(actual.Identifier.Text, expected.Identifier.Text))
            return false;

        // Check if type argument counts match
        var actualArgs = actual.TypeArgumentList.Arguments;
        var expectedArgs = expected.TypeArgumentList.Arguments;

        if (actualArgs.Count != expectedArgs.Count)
            return false;

        // Check each type argument
        for (int i = 0; i < actualArgs.Count; i++)
        {
            if (!AreEquivalent(actualArgs[i], expectedArgs[i]))
                return false;
        }

        return true;
    }

    private static bool AreIdentifiersEquivalent(string actual, string expected)
    {
        // Direct match
        if (string.Equals(actual, expected, StringComparison.Ordinal))
            return true;

        // Check for common type aliases
        return (actual, expected) switch
        {
            (nameof(String), "string") => true,
            ("string", nameof(String)) => true,
            (nameof(Int32), "int") => true,
            ("int", nameof(Int32)) => true,
            (nameof(Boolean), "bool") => true,
            ("bool", nameof(Boolean)) => true,
            (nameof(Object), "object") => true,
            ("object", nameof(Object)) => true,
            (nameof(Decimal), "decimal") => true,
            ("decimal", nameof(Decimal)) => true,
            (nameof(Double), "double") => true,
            ("double", nameof(Double)) => true,
            (nameof(Single), "float") => true,
            ("float", nameof(Single)) => true,
            (nameof(Int64), "long") => true,
            ("long", nameof(Int64)) => true,
            (nameof(Int16), "short") => true,
            ("short", nameof(Int16)) => true,
            (nameof(Byte), "byte") => true,
            ("byte", nameof(Byte)) => true,
            _ => false
        };
    }

    private static bool AreKeywordsEquivalent(SyntaxToken actual, SyntaxToken expected)
    {
        return actual.IsKind(expected.Kind());
    }

    private static bool IsPredefinedTypeEquivalent(PredefinedTypeSyntax predefined, string identifier)
    {
        var token = predefined.Keyword;
        if (token.IsKind(SyntaxKind.StringKeyword)) return string.Equals(identifier, nameof(String), StringComparison.Ordinal);
        if (token.IsKind(SyntaxKind.IntKeyword)) return string.Equals(identifier, nameof(Int32), StringComparison.Ordinal);
        if (token.IsKind(SyntaxKind.BoolKeyword)) return string.Equals(identifier, nameof(Boolean), StringComparison.Ordinal);
        if (token.IsKind(SyntaxKind.ObjectKeyword)) return string.Equals(identifier, nameof(Object), StringComparison.Ordinal);
        if (token.IsKind(SyntaxKind.DecimalKeyword)) return string.Equals(identifier, nameof(Decimal), StringComparison.Ordinal);
        if (token.IsKind(SyntaxKind.DoubleKeyword)) return string.Equals(identifier, nameof(Double), StringComparison.Ordinal);
        if (token.IsKind(SyntaxKind.FloatKeyword)) return string.Equals(identifier, nameof(Single), StringComparison.Ordinal);
        if (token.IsKind(SyntaxKind.LongKeyword)) return string.Equals(identifier, nameof(Int64), StringComparison.Ordinal);
        if (token.IsKind(SyntaxKind.ShortKeyword)) return string.Equals(identifier, nameof(Int16), StringComparison.Ordinal);
        if (token.IsKind(SyntaxKind.ByteKeyword)) return string.Equals(identifier, nameof(Byte), StringComparison.Ordinal);
        return false;
    }
}
