using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Microsoft.Data.SqlTypes;

namespace Fdw.Data.MsSql;

/// <summary>
/// Converts a <c>vector</c> column (FDW-1354): read as <c>float[]</c>, written as its JSON text, which SQL Server converts.
/// </summary>
/// <remarks>
/// SqlClient hands a vector back as <see cref="SqlVector{T}"/> when the server and client agree on vector support, and as
/// its JSON text when they do not; both read as the same <c>float[]</c>. A value that is neither is not a vector, and
/// the read fails rather than guess.
/// </remarks>
[TypeOption(typeof(MsSqlConverters), "Vector", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class MsSqlVectorConverter()
    : DataTypeConverterBase(
        name: "Vector",
        sourceType: "vector",
        targetClrType: typeof(float[]),
        dbType: DbType.String)
{
    /// <inheritdoc/>
    public override object? ToClr(object? dbValue)
    {
        if (dbValue is null or DBNull)
        {
            return null;
        }

        var reader = MsSqlVectorReaders.ByValueTypeName(dbValue.GetType().Name);
        return ReferenceEquals(reader, MsSqlVectorReaders.NotFound)
            ? throw new InvalidOperationException($"A vector column read as {dbValue.GetType().Name}, which is not a vector.")
            : reader.Read(dbValue);
    }

    /// <inheritdoc/>
    public override object? ToDb(object? clrValue) => clrValue is null ? null : DataTypes.Vector.Format(clrValue);
}
