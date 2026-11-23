using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Expressions;

/// <summary>A smallint, held as a <see cref="Int16"/>.</summary>
[TypeOption(typeof(SqlWholeNumbers), "SmallInt")]
[ExcludeFromCodeCoverage]
public sealed class SmallIntSqlWholeNumber : SqlWholeNumberBase
{
    /// <summary>Initializes a new instance of the <see cref="SmallIntSqlWholeNumber"/> class.</summary>
    public SmallIntSqlWholeNumber()
        : base("SmallInt", typeof(short))
    {
    }

    /// <inheritdoc/>
    public override object BitwiseNot(object operand) => ~(short)operand;
}
