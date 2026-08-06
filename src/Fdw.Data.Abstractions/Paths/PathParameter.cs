using System;
using System.Collections.Generic;
using System.Linq;

namespace Fdw.Data.DataStores.Abstractions;

/// <summary>
/// Represents a parameter that can be substituted in a data path.
/// </summary>
/// <remarks>
/// PathParameter defines the metadata for dynamic segments in data paths,
/// including type information, validation rules, and default values.
/// </remarks>
public sealed class PathParameter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PathParameter"/> class.
    /// </summary>
    /// <param name="name">The parameter name.</param>
    /// <param name="parameterType">The expected parameter type.</param>
    /// <param name="isRequired">Whether the parameter is required.</param>
    /// <param name="defaultValue">The default value if the parameter is optional.</param>
    /// <param name="description">A description of the parameter.</param>
    /// <param name="validationRules">Validation rules for the parameter value.</param>
    public PathParameter(
        string name,
        Type parameterType,
        bool isRequired = true,
        object? defaultValue = null,
        string? description = null,
        IEnumerable<string>? validationRules = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ParameterType = parameterType ?? throw new ArgumentNullException(nameof(parameterType));
        IsRequired = isRequired;
        DefaultValue = defaultValue;
        Description = description;
        ValidationRules = validationRules?.ToList() ?? new List<string>();
    }

    /// <summary>
    /// Gets the parameter name.
    /// </summary>
    /// <value>The name used in path templates (e.g., "id" for "{id}").</value>
    public string Name { get; }

    /// <summary>
    /// Gets the expected parameter type.
    /// </summary>
    /// <value>The .NET type that parameter values should be.</value>
    public Type ParameterType { get; }

    /// <summary>
    /// Gets a value indicating whether this parameter is required.
    /// </summary>
    /// <value><c>true</c> if required; otherwise, <c>false</c>.</value>
    public bool IsRequired { get; }

    /// <summary>
    /// Gets the default value for optional parameters.
    /// </summary>
    /// <value>The default value, or null if no default is specified.</value>
    public object? DefaultValue { get; }

    /// <summary>
    /// Gets the parameter description.
    /// </summary>
    /// <value>A human-readable description of the parameter purpose.</value>
    public string? Description { get; }

    /// <summary>
    /// Gets validation rules for the parameter value.
    /// </summary>
    /// <value>A list of validation rule descriptions.</value>
    public IReadOnlyList<string> ValidationRules { get; }
}