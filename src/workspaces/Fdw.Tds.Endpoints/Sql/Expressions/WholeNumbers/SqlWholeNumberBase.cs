using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Expressions;

/// <summary>The base of every .NET type a T-SQL whole number is held in.</summary>
[ExcludeFromCodeCoverage]
public abstract class SqlWholeNumberBase : TypeOptionBase<SqlWholeNumberBase>, ISqlWholeNumber
{
    /// <summary>Initializes a new instance of the <see cref="SqlWholeNumberBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    /// <param name="clrType">The .NET type the number is held in.</param>
    protected SqlWholeNumberBase(string name, Type clrType)
        : base(name)
    {
        ClrType = clrType;
    }

    /// <summary>Initializes a new instance of the <see cref="SqlWholeNumberBase"/> class for the generated NotFound sentinel, which holds no type.</summary>
    protected SqlWholeNumberBase()
        : base("NotFound")
    {
        ClrType = typeof(object);
    }

    /// <inheritdoc/>
    [TypeLookup("ByClrType")]
    public Type ClrType { get; }

    /// <inheritdoc/>
    public abstract object BitwiseNot(object operand);
}
