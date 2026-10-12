using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Collections.Attributes;

/// <summary>
/// Marks a property for which to generate lookup methods.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class TypeLookupAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeLookupAttribute"/> class.
    /// </summary>
    /// <param name="methodName">Required method name for the lookup (e.g. "Name", "Id", "Category").</param>
    /// <param name="returnType">The return type for this specific lookup method.</param>
    /// <param name="isUnique">True (the default) when at most one option can carry a given value for this
    /// property, so the lookup returns that option; false when several can, so it returns all of them.</param>
    public TypeLookupAttribute(
        string methodName,
        Type? returnType = null,
        bool isUnique = true)
    {
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        ReturnType = returnType;
        IsUnique = isUnique;
    }

    /// <summary>
    /// Gets the method name for the lookup (e.g. "Name", "Id", "Category").
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Gets whether a value for this property identifies at most one option.
    /// </summary>
    /// <remarks>
    /// Why the default is true: uniqueness is the promise the collection ENFORCES, so it is the loud
    /// option rather than the permissive one. Two options arriving with the same value for a unique
    /// property is a registration error that throws and names both; the same collision under a
    /// non-unique lookup is a two-element list that nobody inspects and every caller silently reads the
    /// first of. Defaulting to true also matches what every lookup already did before this parameter was
    /// read by anything, so declaring nothing keeps the behaviour a caller already has.
    /// </remarks>
    public bool IsUnique { get; }

    /// <summary>
    /// Gets the return type for this specific lookup method.
    /// If not specified, inherits from the TypeCollection attribute ReturnType or auto-detected type.
    /// </summary>
    public Type? ReturnType { get; }
}