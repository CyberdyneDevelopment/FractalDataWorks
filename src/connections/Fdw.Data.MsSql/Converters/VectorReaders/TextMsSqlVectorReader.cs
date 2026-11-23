using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>Reads a vector that came back as its JSON text.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlVectorReaders), "Text")]
public sealed class TextMsSqlVectorReader : MsSqlVectorReaderBase
{
    /// <summary>Initializes a new instance of the <see cref="TextMsSqlVectorReader"/> class.</summary>
    public TextMsSqlVectorReader()
        : base("Text", "String")
    {
    }

    /// <inheritdoc/>
    public override object? Read(object dbValue) =>
        dbValue is string text ? DataTypes.Vector.Parse(text) ?? throw new InvalidOperationException($"The vector column's text is not a vector: '{text}'.") : throw WrongType(dbValue);

    private static InvalidOperationException WrongType(object dbValue) =>
        new($"A vector column read as {dbValue.GetType().Name}, which is not a vector.");
}
