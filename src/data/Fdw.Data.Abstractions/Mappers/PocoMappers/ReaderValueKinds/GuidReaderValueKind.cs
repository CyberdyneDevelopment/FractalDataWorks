using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions.Mappers.PocoMappers;

/// <summary>A GUID.</summary>
[TypeOption(typeof(ReaderValueKinds), "Guid")]
[ExcludeFromCodeCoverage]
public sealed class GuidReaderValueKind : ReaderValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="GuidReaderValueKind"/> class.</summary>
    public GuidReaderValueKind()
        : base("Guid")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is Guid;

    /// <inheritdoc />
    public override Guid ToGuid(object value, string column) => (Guid)value;
}
