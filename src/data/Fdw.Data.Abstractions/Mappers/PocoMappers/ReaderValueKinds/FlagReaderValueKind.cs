using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions.Mappers.PocoMappers;

/// <summary>True or false.</summary>
[TypeOption(typeof(ReaderValueKinds), "Flag")]
[ExcludeFromCodeCoverage]
public sealed class FlagReaderValueKind : ReaderValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FlagReaderValueKind"/> class.</summary>
    public FlagReaderValueKind()
        : base("Flag")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is bool;

    /// <inheritdoc />
    public override bool ToBoolean(object value) => (bool)value;
}
