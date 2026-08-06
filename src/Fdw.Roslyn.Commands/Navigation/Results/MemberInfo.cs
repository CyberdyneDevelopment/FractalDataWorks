using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Navigation.Results;

/// <summary>
/// Represents information about a type member.
/// </summary>
public sealed record MemberInfo
{
    /// <summary>
    /// Gets or sets the member name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the member kind (e.g., Method, Property, Field).
    /// </summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the accessibility (e.g., Public, Private).
    /// </summary>
    public string Accessibility { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the member is static.
    /// </summary>
    public bool IsStatic { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the member is abstract.
    /// </summary>
    public bool IsAbstract { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the member is virtual.
    /// </summary>
    public bool IsVirtual { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the member is an override.
    /// </summary>
    public bool IsOverride { get; init; }

    /// <summary>
    /// Gets or sets the return type for methods.
    /// </summary>
    public string? ReturnType { get; init; }

    /// <summary>
    /// Gets or sets the parameters for methods.
    /// </summary>
    public IReadOnlyList<string>? Parameters { get; init; }

    /// <summary>
    /// Gets or sets the property type.
    /// </summary>
    public string? PropertyType { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the property has a getter.
    /// </summary>
    public bool? HasGetter { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the property has a setter.
    /// </summary>
    public bool? HasSetter { get; init; }

    /// <summary>
    /// Gets or sets the field type.
    /// </summary>
    public string? FieldType { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the field is read-only.
    /// </summary>
    public bool? IsReadOnly { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the field is const.
    /// </summary>
    public bool? IsConst { get; init; }

    /// <summary>
    /// Gets or sets the file path where the member is located.
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets or sets the line number (1-based).
    /// </summary>
    public int? Line { get; init; }

    /// <summary>
    /// Gets or sets the column number (1-based).
    /// </summary>
    public int? Column { get; init; }
}
