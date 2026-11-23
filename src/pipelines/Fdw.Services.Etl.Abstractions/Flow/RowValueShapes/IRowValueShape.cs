using System;
using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>A type of value a row can hold, and how much memory a value of it is estimated to hold.</summary>
public interface IRowValueShape : ITypeOption<int, RowValueShapeBase>
{
    /// <summary>Gets the exact type of the values this shape sizes.</summary>
    Type ClrType { get; }

    /// <summary>Estimates the size of a value of this type.</summary>
    /// <param name="value">A value of <see cref="ClrType"/>.</param>
    /// <returns>The estimated bytes.</returns>
    long SizeOf(object value);
}
