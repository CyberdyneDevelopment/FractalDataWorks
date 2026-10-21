using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Commands.Data.Abstractions;

namespace Fdw.Commands.Data;

/// <summary>
/// The rows of a container nearest a vector, measured on one of its vector fields: each row with its distance, the
/// nearest first (FDW-1354).
/// </summary>
/// <typeparam name="T">The type each row is read as.</typeparam>
/// <remarks>
/// <para>
/// A vectorized command: only a store that holds vectors answers it, and the DataGateway refuses it for a store whose
/// connection is not vectorized. The vector field itself is not read back; the distance stands for it.
/// </para>
/// <para>
/// Example usage:
/// <code>
/// var call = Vectorized.Nearest&lt;Passage&gt;("TechComDb", "tc", "ConversationPassage")
///     .To("Vector", question)
///     .Metric(VectorMetrics.Cosine)
///     .Take(10)
///     .Build();
/// if (call.IsFailure) return call.ToNewResult&lt;...&gt;();
/// var nearest = await gateway.Execute(call.Value!, ct);
/// </code>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(VectorizedDataCommands), "Nearest", RestrictToCurrentCompilation = true)]
public sealed class NearestCommand<T> : VectorizedDataCommandBase<IReadOnlyList<NearestResult<T>>>, INearestCommand
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NearestCommand{T}"/> class.
    /// </summary>
    public NearestCommand()
        : base("Nearest")
    {
    }

    /// <inheritdoc/>
    public required string VectorField { get; init; }

    /// <inheritdoc/>
    public required IReadOnlyList<float> Vector { get; init; }

    /// <inheritdoc/>
    public required IVectorMetric Metric { get; init; }

    /// <inheritdoc/>
    public required int Take { get; init; }

    /// <inheritdoc/>
    public Type RowType => typeof(T);

    /// <inheritdoc/>
    public object Results(IReadOnlyList<object> rows, IReadOnlyList<double> distances)
    {
        var results = new List<NearestResult<T>>(rows.Count);
        for (var i = 0; i < rows.Count; i++)
            results.Add(new NearestResult<T> { Record = (T)rows[i], Distance = distances[i] });

        return results;
    }
}
