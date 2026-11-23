using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.MsSql;

/// <summary>Reads a vector that came back as a float array.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlVectorReaders), "FloatArray")]
public sealed class FloatArrayMsSqlVectorReader : MsSqlVectorReaderBase
{
    /// <summary>Initializes a new instance of the <see cref="FloatArrayMsSqlVectorReader"/> class.</summary>
    public FloatArrayMsSqlVectorReader()
        : base("FloatArray", "Single[]")
    {
    }

    /// <inheritdoc/>
    public override object? Read(object dbValue) =>
        dbValue is float[] values ? values : throw WrongType(dbValue);

    private static InvalidOperationException WrongType(object dbValue) =>
        new($"A vector column read as {dbValue.GetType().Name}, which is not a vector.");
}
