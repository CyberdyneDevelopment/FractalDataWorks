using System;
using System.Diagnostics.CodeAnalysis;

namespace Fdw.Configuration;

/// <summary>
/// Specifies that the values for this property should be sourced from a TypeCollection.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute on discriminator properties (e.g., ServiceOptionType) to indicate
/// which TypeCollection provides the valid values. The UI can use this to render a dropdown
/// or selection list of available TypeOption names.
/// </para>
/// <para>
/// The referenced TypeCollection type must be accessible from the project where this
/// attribute is applied. If not, add the appropriate project reference.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Type-safe reference (when TypeCollection assembly is available):
/// [ValuesFrom(typeof(ConnectionTypes))]
/// public string ServiceOptionType { get; set; } = string.Empty;
///
/// // String-based reference (for client DTOs without assembly dependency):
/// [ValuesFrom("CalculationTypes")]
/// public string CalculationType { get; set; } = string.Empty;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
[ExcludeFromCodeCoverage]
public sealed class ValuesFromAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValuesFromAttribute"/> class
    /// with a compile-time type reference to the TypeCollection.
    /// </summary>
    /// <param name="typeCollectionType">The TypeCollection type that provides valid values.</param>
    public ValuesFromAttribute(Type typeCollectionType)
    {
        TypeCollectionType = typeCollectionType ?? throw new ArgumentNullException(nameof(typeCollectionType));
        TypeCollectionName = typeCollectionType.Name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValuesFromAttribute"/> class
    /// with a string-based name reference. Use this when the TypeCollection assembly
    /// is not available at compile time (e.g., in client DTO packages).
    /// </summary>
    /// <param name="typeCollectionName">The name of the TypeCollection (e.g., "CalculationTypes").</param>
    public ValuesFromAttribute(string typeCollectionName)
    {
        TypeCollectionName = typeCollectionName ?? throw new ArgumentNullException(nameof(typeCollectionName));
    }

    /// <summary>
    /// Gets the TypeCollection type that provides valid values for this property.
    /// Null when constructed with the string-based overload.
    /// </summary>
    public Type? TypeCollectionType { get; }

    /// <summary>
    /// Gets the name of the TypeCollection. Always populated regardless of constructor used.
    /// </summary>
    public string TypeCollectionName { get; }

    /// <summary>
    /// Gets or sets the property name on the TypeOption to use for display.
    /// </summary>
    /// <remarks>
    /// If not specified, uses the Name property of the TypeOption.
    /// </remarks>
    public string? DisplayProperty { get; set; }
}
