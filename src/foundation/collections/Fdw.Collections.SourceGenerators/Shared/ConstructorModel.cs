using System.Collections.Immutable;

namespace Fdw.Collections.SourceGenerators.Shared;

/// <summary>
/// Model for a public constructor.
/// </summary>
internal readonly record struct ConstructorModel(
    ImmutableArray<ParameterModel> Parameters
)
{
    public bool IsParameterless => Parameters.Length == 0;
    public bool HasParameters => Parameters.Length > 0;
}