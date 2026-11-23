using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.ColumnValues;

/// <summary>A 32-bit whole number.</summary>
[TypeOption(typeof(RowCountKinds), "Int32")]
[ExcludeFromCodeCoverage]
public sealed class Int32RowCountKind : RowCountKindBase
{
    /// <summary>Initializes a new instance of the <see cref="Int32RowCountKind"/> class.</summary>
    public Int32RowCountKind()
        : base("Int32")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is int;

    /// <inheritdoc />
    public override long? Read(object value) => (int)value;
}
