using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Expressions;

/// <summary>A int, held as a <see cref="Int32"/>.</summary>
[TypeOption(typeof(SqlWholeNumbers), "Int")]
[ExcludeFromCodeCoverage]
public sealed class IntSqlWholeNumber : SqlWholeNumberBase
{
    /// <summary>Initializes a new instance of the <see cref="IntSqlWholeNumber"/> class.</summary>
    public IntSqlWholeNumber()
        : base("Int", typeof(int))
    {
    }

    /// <inheritdoc/>
    public override object BitwiseNot(object operand) => ~(int)operand;
}
