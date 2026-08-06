using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Collections.Attributes;

/// <summary>
/// Marks a concrete type option with explicit collection targeting.
/// </summary>
// Why: pure attribute definition (declarative metadata only, consumed by source generators) — no logic to unit test.
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class TypeOptionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeOptionAttribute"/> class.
    /// </summary>
    /// <param name="collectionType">The type of the collection this option belongs to.</param>
    /// <param name="name">The name for the method/property in the generated collection.</param>
    public TypeOptionAttribute(Type collectionType, string name)
    {
        CollectionType = collectionType ?? throw new ArgumentNullException(nameof(collectionType));
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>
    /// Gets the type of the collection this option belongs to.
    /// </summary>
    public Type CollectionType { get; }

    /// <summary>
    /// Gets the name for the method/property in the generated collection.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets a value indicating whether to restrict this TypeOption to the current compilation only.
    /// When true, this option will not be registered via module initializer in consuming assemblies.
    /// When false, the Registration.SourceGenerators will generate module initializers in entry points.
    /// Default is false (cross-assembly registration enabled).
    /// </summary>
    /// <remarks>
    /// <para><b>Use true when:</b> This TypeOption is only used within the same assembly as the TypeCollection</para>
    /// <para><b>Use false when:</b> This TypeOption needs to be discovered by entry point applications</para>
    /// <para>
    /// For cross-assembly registration to work, both the TypeCollection and TypeOption must have
    /// RestrictToCurrentCompilation = false (the default), and the entry point must reference
    /// Fdw.Registration.SourceGenerators.
    /// </para>
    /// </remarks>
    public bool RestrictToCurrentCompilation { get; set; }

    /// <summary>
    /// Gets or sets the category for UI grouping and organization.
    /// </summary>
    /// <remarks>
    /// Categories enable TypeCollection dropdowns to group related options under headings.
    /// Example: "Streaming", "Batch", "Memory" for RowMapperTypes.
    /// </remarks>
    public string? Category { get; set; }
}