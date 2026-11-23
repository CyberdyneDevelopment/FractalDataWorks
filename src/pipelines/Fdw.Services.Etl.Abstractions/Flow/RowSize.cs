using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>
/// Estimates the size of the rows that move over a link (FDW-3381).
/// </summary>
/// <remarks>
/// <para>
/// The size is an estimate of the memory the values of a row hold: text counts two bytes a character, a number its width, a date eight bytes, a byte array its
/// length. It is not the size on disk or on the wire, and a value of a type not listed counts eight bytes. A column name counts nothing: it is the same in every row. The
/// estimate is the same whatever receives the rows, so the figures of two runs can be compared.
/// </para>
/// </remarks>
public static class RowSize
{
    /// <summary>The size a value of a type not listed here counts.</summary>
    public const int OtherValue = 8;

    /// <summary>Estimates the size of one row's values.</summary>
    /// <param name="row">The row.</param>
    /// <returns>The estimated bytes.</returns>
    public static long Of(IDictionary<string, object?> row)
    {
        long total = 0;
        foreach (var value in row.Values)
        {
            total += Of(value);
        }

        return total;
    }

    /// <summary>Estimates the size of every row of a batch.</summary>
    /// <param name="batch">The batch.</param>
    /// <returns>The estimated bytes.</returns>
    public static long Of(FlowBatch batch)
    {
        long total = 0;
        foreach (var row in batch.Rows)
        {
            total += Of(row);
        }

        return total;
    }

    /// <summary>Estimates the size of one value.</summary>
    /// <param name="value">The value; null counts nothing.</param>
    /// <returns>The estimated bytes.</returns>
    public static long Of(object? value) =>
        value is null ? 0 : RowValueShapes.ByClrType(value.GetType()) is var shape && !ReferenceEquals(shape, RowValueShapes.NotFound) ? shape.SizeOf(value) : OtherValue;
}
