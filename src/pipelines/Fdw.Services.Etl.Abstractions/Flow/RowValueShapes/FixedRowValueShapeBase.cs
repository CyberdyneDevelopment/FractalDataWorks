using System;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Base class for the types of value whose every value is estimated to hold the same number of bytes.</summary>
public abstract class FixedRowValueShapeBase : RowValueShapeBase
{
    private readonly long _bytes;

    /// <summary>Initializes a new instance of the <see cref="FixedRowValueShapeBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="clrType">The exact type of the values this shape sizes.</param>
    /// <param name="bytes">The estimated bytes of every value of the type.</param>
    protected FixedRowValueShapeBase(string name, Type clrType, long bytes)
        : base(name, clrType)
    {
        _bytes = bytes;
    }

    /// <inheritdoc />
    public sealed override long SizeOf(object value) => _bytes;
}
