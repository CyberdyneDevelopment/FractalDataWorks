using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Data.Transformations;

/// <summary>The base of every option of <see cref="TimeZoneSourceKinds"/>.</summary>
[ExcludeFromCodeCoverage]
public abstract class TimeZoneSourceKindBase : TypeOptionBase<TimeZoneSourceKindBase>, ITimeZoneSourceKind
{
    /// <summary>Initializes a new instance of the <see cref="TimeZoneSourceKindBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    protected TimeZoneSourceKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Holds(object value);

    /// <inheritdoc />
    public abstract DateTimeOffset InZone(object value, TimeZoneInfo timeZone);
}
