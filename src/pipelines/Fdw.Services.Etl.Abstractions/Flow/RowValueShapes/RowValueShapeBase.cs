using System;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>Base class for the types of value a row can hold and how much memory a value of each is estimated to hold.</summary>
public abstract class RowValueShapeBase : TypeOptionBase<RowValueShapeBase>, IRowValueShape
{
    /// <summary>Initializes a new instance of the <see cref="RowValueShapeBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="clrType">The exact type of the values this shape sizes.</param>
    protected RowValueShapeBase(string name, Type clrType)
        : base(name)
    {
        ClrType = clrType;
    }

    /// <inheritdoc />
    [TypeLookup("ByClrType")]
    public Type ClrType { get; }

    /// <inheritdoc />
    public abstract long SizeOf(object value);
}
