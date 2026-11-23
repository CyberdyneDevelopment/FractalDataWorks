using System;
using Fdw.Collections;

namespace Fdw.Services.Etl.Tracking;

/// <summary>Base class for the shapes a value in a run event's data can have. A shape reads as nothing until it says it reads as something.</summary>
public abstract class EventValueShapeBase : TypeOptionBase<EventValueShapeBase>, IEventValueShape
{
    /// <summary>Initializes a new instance of the <see cref="EventValueShapeBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    protected EventValueShapeBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Matches(object value);

    /// <inheritdoc />
    public virtual long? Number(object value) => null;

    /// <inheritdoc />
    public virtual bool? Flag(object value) => null;

    /// <inheritdoc />
    public virtual DateTimeOffset? Time(object value) => null;

    /// <inheritdoc />
    public virtual string? Text(object value) => null;
}
