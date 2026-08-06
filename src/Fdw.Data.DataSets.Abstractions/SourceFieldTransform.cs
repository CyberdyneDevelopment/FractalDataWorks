using System.Collections.Generic;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// A single step in a field mapping's transform chain.
/// Carries the transform type name and its configured parameter values.
/// </summary>
/// <remarks>
/// Execution is dispatched via <c>DataTransformerTypes.ByName(TransformType)</c> — no switch over type names.
/// </remarks>
public sealed class SourceFieldTransform
{
    /// <summary>
    /// Gets or sets the transform type name — must match a registered <c>DataTransformerTypes</c> option.
    /// </summary>
    public string TransformType { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameter values for this transform step, keyed by parameter name.
    /// </summary>
    public IReadOnlyDictionary<string, string> Parameters { get; init; } =
        new Dictionary<string, string>(System.StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the ordinal position of this step in the chain (ascending = applied first).
    /// </summary>
    public int Ordinal { get; init; }
}
