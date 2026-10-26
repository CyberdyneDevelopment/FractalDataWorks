using System;
using Fdw.Collections;

namespace Fdw.Services.Calculations.Abstractions.RelativeTime;

/// <summary>
/// Base class for the anchors the current moment can be cut back to (CRTP pattern).
/// </summary>
public abstract class TimeAnchorBase : TypeOptionBase<TimeAnchorBase>, ITimeAnchor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeAnchorBase"/> class.
    /// </summary>
    /// <param name="name">The anchor's name, as a node states it.</param>
    /// <param name="summary">A one-sentence description.</param>
    protected TimeAnchorBase(string name, string summary)
        : base(name)
    {
        Summary = summary;
    }

    /// <inheritdoc />
    public string Summary { get; }

    /// <inheritdoc />
    public abstract DateTimeOffset Apply(DateTimeOffset moment, TimeZoneInfo zone);
}
