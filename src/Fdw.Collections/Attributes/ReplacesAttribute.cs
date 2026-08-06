using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Collections.Attributes;

/// <summary>
/// Declares that this TypeOption replaces another in its collection.
/// The source generators will omit the original from registration and register this type instead.
/// </summary>
/// <remarks>
/// Why: Enables downstream assemblies to swap TypeOption implementations without modifying FDW source.
/// The executable's module initializer has full visibility across all referenced assemblies, so it
/// builds the complete replacement map before emitting any RegisterMember calls.
/// </remarks>
// Why: pure attribute definition (declarative metadata only, consumed by source generators) — no logic to unit test.
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
[ExcludeFromCodeCoverage]
public sealed class ReplacesAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReplacesAttribute"/> class.
    /// </summary>
    /// <param name="originalType">The TypeOption type to replace.</param>
    public ReplacesAttribute(Type originalType)
    {
        OriginalType = originalType ?? throw new ArgumentNullException(nameof(originalType));
    }

    /// <summary>
    /// Gets the TypeOption type being replaced.
    /// </summary>
    public Type OriginalType { get; }
}
