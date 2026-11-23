using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections;

namespace Fdw.Services.Data.ColumnValues;

/// <summary>The base of every option of <see cref="RowCountKinds"/>.</summary>
[ExcludeFromCodeCoverage]
public abstract class RowCountKindBase : TypeOptionBase<RowCountKindBase>, IRowCountKind
{
    /// <summary>Initializes a new instance of the <see cref="RowCountKindBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    protected RowCountKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Holds(object value);

    /// <inheritdoc />
    public abstract long? Read(object value);
}
