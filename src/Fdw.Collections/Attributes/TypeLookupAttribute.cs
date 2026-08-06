using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Collections.Attributes;

/// <summary>
/// Marks a property for which to generate lookup methods.
/// </summary>
// Why: pure attribute definition (declarative metadata only, consumed by source generators) — no logic to unit test.
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class TypeLookupAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeLookupAttribute"/> class.
    /// </summary>
    /// <param name="methodName">Required method name for the lookup (e.g. "Name", "Id", "Category").</param>
    /// <param name="returnsList">If true, returns IReadOnlyList of matching items; if false, returns single item (default).</param>
    /// <param name="returnType">The return type for this specific lookup method.</param>
    public TypeLookupAttribute(
        string methodName,
        bool returnsList = false,
        Type? returnType = null)
    {
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        ReturnsList = returnsList;
        ReturnType = returnType;
    }

    /// <summary>
    /// Gets the method name for the lookup (e.g. "Name", "Id", "Category").
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Gets whether to return a list of items instead of a single item.
    /// </summary>
    public bool ReturnsList { get; }

    /// <summary>
    /// Gets the return type for this specific lookup method.
    /// If not specified, inherits from the TypeCollection attribute ReturnType or auto-detected type.
    /// </summary>
    public Type? ReturnType { get; }
}