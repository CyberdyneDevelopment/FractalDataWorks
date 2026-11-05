namespace Fdw.Collections.SourceGenerators.Shared;

/// <summary>
/// Model for a constructor parameter.
/// </summary>
/// <param name="Name">The parameter name.</param>
/// <param name="Type">The parameter type, as Roslyn displays it.</param>
/// <param name="HasDefaultValue">Whether the parameter declares a default value.</param>
/// <param name="DefaultValue">The declared default value, formatted for code.</param>
/// <param name="IsValueType">Whether the type is a value type, so default(T) is a value and never null.</param>
internal readonly record struct ParameterModel(
    string Name,
    string Type,
    bool HasDefaultValue,
    string DefaultValue,
    bool IsValueType
);