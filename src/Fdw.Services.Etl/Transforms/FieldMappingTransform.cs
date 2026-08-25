using System.Collections.Generic;
using Fdw.Services.Etl.Abstractions;

namespace Fdw.Services.Etl;

/// <summary>
/// One step in a field mapping's transform chain.
/// </summary>
/// <remarks>
/// Why parameters are supplied at construction and never defaulted afterwards: a step either was
/// configured with values or it was not, and the difference has to survive to the point of execution.
/// A step built with no parameters reports exactly that, so a transform requiring one fails loud
/// instead of silently running with an empty bag.
/// </remarks>
internal sealed class FieldMappingTransform : IFieldMappingTransform
{
    /// <summary>Initializes a new instance of the <see cref="FieldMappingTransform"/> class.</summary>
    /// <param name="transformType">The registered transform name to run.</param>
    /// <param name="ordinal">Position in the chain; ascending order is applied first.</param>
    /// <param name="parameters">The configured parameter values, or null when none were configured.</param>
    public FieldMappingTransform(
        string transformType,
        int ordinal,
        IReadOnlyDictionary<string, string>? parameters = null)
    {
        TransformType = transformType;
        Ordinal = ordinal;
        Parameters = parameters ?? EmptyParameters;
    }

    /// <summary>The shared empty bag, so "nothing was configured" allocates nothing per step.</summary>
    private static readonly Dictionary<string, string> EmptyParameters = new(System.StringComparer.Ordinal);

    /// <inheritdoc/>
    public string TransformType { get; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Parameters { get; }

    /// <inheritdoc/>
    public int Ordinal { get; }
}
