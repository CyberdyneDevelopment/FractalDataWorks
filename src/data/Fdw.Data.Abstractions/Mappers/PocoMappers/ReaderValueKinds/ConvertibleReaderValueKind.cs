using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions.Mappers.PocoMappers;

/// <summary>Any other value, read through the framework conversions as a data reader reads it.</summary>
[TypeOption(typeof(ReaderValueKinds), "Convertible")]
[ExcludeFromCodeCoverage]
public sealed class ConvertibleReaderValueKind : ReaderValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ConvertibleReaderValueKind"/> class.</summary>
    public ConvertibleReaderValueKind()
        : base("Convertible")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is not (Guid or string or bool or long or double or decimal or DateTime or DateTimeOffset);
}
