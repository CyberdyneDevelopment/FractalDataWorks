using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections.Attributes;

namespace Fdw.Data.RowSources.Abstractions;

/// <summary>A GUID, in the hyphenated form.</summary>
[TypeOption(typeof(RecordValueKinds), "Identifier")]
[ExcludeFromCodeCoverage]
public sealed class IdentifierRecordValueKind : RecordValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="IdentifierRecordValueKind"/> class.</summary>
    public IdentifierRecordValueKind()
        : base("Identifier")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is Guid;

    /// <inheritdoc />
    public override string Text(object value) => ((Guid)value).ToString("D", CultureInfo.InvariantCulture);
}
