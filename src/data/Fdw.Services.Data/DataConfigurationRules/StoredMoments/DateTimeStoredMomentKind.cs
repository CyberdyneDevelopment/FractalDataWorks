using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.DataConfigurationRules;

/// <summary>A date and time.</summary>
[TypeOption(typeof(StoredMomentKinds), "DateTime")]
[ExcludeFromCodeCoverage]
public sealed class DateTimeStoredMomentKind : StoredMomentKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeStoredMomentKind"/> class.</summary>
    public DateTimeStoredMomentKind()
        : base("DateTime")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is DateTime;

    /// <inheritdoc />
    public override DateTimeOffset? Read(object value) => new DateTimeOffset((DateTime)value);
}
