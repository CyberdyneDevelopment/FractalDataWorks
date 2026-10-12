using System;

namespace Fdw.Commands.Development.Abstractions;

/// <summary>
/// Describes a parameter for a development command.
/// </summary>
public sealed class DevelopmentCommandParameter
{
    /// <summary>
    /// Gets the parameter name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the parameter description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the parameter type.
    /// </summary>
    public Type ParameterType { get; }

    /// <summary>
    /// Gets whether the parameter is required.
    /// </summary>
    public bool IsRequired { get; }

    /// <summary>
    /// Gets the default value if not required.
    /// </summary>
    public object? DefaultValue { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="DevelopmentCommandParameter"/>.
    /// </summary>
    public DevelopmentCommandParameter(
        string name,
        string description,
        Type parameterType,
        bool isRequired = true,
        object? defaultValue = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        ParameterType = parameterType ?? throw new ArgumentNullException(nameof(parameterType));
        IsRequired = isRequired;
        DefaultValue = defaultValue;
    }

    /// <summary>
    /// Creates a required string parameter.
    /// </summary>
    public static DevelopmentCommandParameter RequiredString(string name, string description) =>
        new(name, description, typeof(string), isRequired: true);

    /// <summary>
    /// Creates an optional string parameter.
    /// </summary>
    public static DevelopmentCommandParameter OptionalString(string name, string description, string? defaultValue = null) =>
        new(name, description, typeof(string), isRequired: false, defaultValue);

    /// <summary>
    /// Creates a required integer parameter.
    /// </summary>
    public static DevelopmentCommandParameter RequiredInt(string name, string description) =>
        new(name, description, typeof(int), isRequired: true);

    /// <summary>
    /// Creates an optional integer parameter.
    /// </summary>
    public static DevelopmentCommandParameter OptionalInt(string name, string description, int defaultValue = 0) =>
        new(name, description, typeof(int), isRequired: false, defaultValue);

    /// <summary>
    /// Creates a required boolean parameter.
    /// </summary>
    public static DevelopmentCommandParameter RequiredBool(string name, string description) =>
        new(name, description, typeof(bool), isRequired: true);

    /// <summary>
    /// Creates an optional boolean parameter.
    /// </summary>
    public static DevelopmentCommandParameter OptionalBool(string name, string description, bool defaultValue = false) =>
        new(name, description, typeof(bool), isRequired: false, defaultValue);
}
