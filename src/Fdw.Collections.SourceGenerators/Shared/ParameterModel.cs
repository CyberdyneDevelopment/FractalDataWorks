namespace Fdw.Collections.SourceGenerators.Shared;

/// <summary>
/// Model for a constructor parameter.
/// </summary>
internal readonly record struct ParameterModel(
    string Name,
    string Type,
    bool HasDefaultValue,
    string DefaultValue
);