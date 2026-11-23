using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Microsoft.Data.SqlTypes;

namespace Fdw.Data.MsSql;

/// <summary>Reads a vector that came back as a SqlVector.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlVectorReaders), "SqlVector")]
public sealed class SqlVectorMsSqlVectorReader : MsSqlVectorReaderBase
{
    /// <summary>Initializes a new instance of the <see cref="SqlVectorMsSqlVectorReader"/> class.</summary>
    public SqlVectorMsSqlVectorReader()
        : base("SqlVector", "SqlVector`1")
    {
    }

    /// <inheritdoc/>
    public override object? Read(object dbValue) =>
        dbValue is SqlVector<float> vector ? (vector.IsNull ? null : vector.Memory.ToArray()) : throw WrongType(dbValue);

    private static InvalidOperationException WrongType(object dbValue) =>
        new($"A vector column read as {dbValue.GetType().Name}, which is not a vector.");
}
