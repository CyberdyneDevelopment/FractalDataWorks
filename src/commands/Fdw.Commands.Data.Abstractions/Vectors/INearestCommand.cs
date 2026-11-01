using System;
using System.Collections.Generic;

namespace Fdw.Commands.Data.Abstractions;

/// <summary>
/// The rows of a container nearest a vector, measured on one of its vector fields: each row with its distance, the
/// nearest first.
/// </summary>
/// <remarks>
/// A connection reads each row as <see cref="RowType"/> and its distance, and hands both to <see cref="Results"/>, which
/// builds the command's own result, so the connection needs no knowledge of it.
/// </remarks>
public interface INearestCommand : IVectorizedDataCommand
{
    /// <summary>Gets the vector field the distance is measured on.</summary>
    string VectorField { get; }

    /// <summary>Gets the vector the distance is measured from.</summary>
    IReadOnlyList<float> Vector { get; }

    /// <summary>Gets how the distance is measured.</summary>
    IVectorMetric Metric { get; }

    /// <summary>Gets how many rows to answer with: the nearest this many.</summary>
    int Take { get; }

    /// <summary>Gets the type each row is read as.</summary>
    Type RowType { get; }

    /// <summary>The command's result from the rows read and their distances, in the order read.</summary>
    /// <param name="rows">Each row, read as <see cref="RowType"/>.</param>
    /// <param name="distances">Each row's distance, by position.</param>
    /// <returns>The command's result.</returns>
    object Results(IReadOnlyList<object> rows, IReadOnlyList<double> distances);
}
