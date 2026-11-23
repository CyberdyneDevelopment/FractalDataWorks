using System;
using Fdw.Collections;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>Base class for the shapes a value in a trigger's configuration can have. A shape reads as nothing until it says it reads as something.</summary>
public abstract class TriggerValueShapeBase : TypeOptionBase<TriggerValueShapeBase>, ITriggerValueShape
{
    /// <summary>Initializes a new instance of the <see cref="TriggerValueShapeBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    protected TriggerValueShapeBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Matches(object value);

    /// <inheritdoc />
    public virtual bool TryInt(object value, out int result)
    {
        result = 0;
        return false;
    }

    /// <inheritdoc />
    public virtual bool TryWindowInt(object value, out int result) => TryInt(value, out result);

    /// <inheritdoc />
    public virtual bool TryNarrowInt(object value, out int result)
    {
        result = 0;
        return false;
    }

    /// <inheritdoc />
    public virtual bool TryDateTime(object value, out DateTime result)
    {
        result = default;
        return false;
    }

    /// <inheritdoc />
    public virtual bool TryBool(object value, out bool result)
    {
        result = false;
        return false;
    }
}
