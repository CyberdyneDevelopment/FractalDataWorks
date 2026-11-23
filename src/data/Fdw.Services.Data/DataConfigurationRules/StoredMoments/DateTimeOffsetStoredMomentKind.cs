using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.DataConfigurationRules;

/// <summary>A date and time with an offset.</summary>
[TypeOption(typeof(StoredMomentKinds), "DateTimeOffset")]
[ExcludeFromCodeCoverage]
public sealed class DateTimeOffsetStoredMomentKind : StoredMomentKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeOffsetStoredMomentKind"/> class.</summary>
    public DateTimeOffsetStoredMomentKind()
        : base("DateTimeOffset")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is DateTimeOffset;

    /// <inheritdoc />
    public override DateTimeOffset? Read(object value) => (DateTimeOffset)value;
}
