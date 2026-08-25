using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions;

/// <summary>
/// One step in a field mapping's transform chain: which transform to run, and what it was
/// configured with.
/// </summary>
/// <remarks>
/// Dispatch is by name through <c>DataTransformerTypes.ByName(TransformType)</c> — there is no
/// switch over transform names anywhere, and adding a transform means adding a TypeOption.
/// </remarks>
public interface IFieldMappingTransform
{
    /// <summary>
    /// Gets the transform type name — must match a registered <c>DataTransformerTypes</c> option.
    /// </summary>
    string TransformType { get; }

    /// <summary>
    /// Gets the configured parameter values for this step, keyed by parameter name.
    /// </summary>
    IReadOnlyDictionary<string, string> Parameters { get; }

    /// <summary>
    /// Gets the position of this step in the chain; ascending order is applied first.
    /// </summary>
    int Ordinal { get; }
}
