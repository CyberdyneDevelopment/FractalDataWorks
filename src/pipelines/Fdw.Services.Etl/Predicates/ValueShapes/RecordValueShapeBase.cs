using System;
using System.Globalization;
using Fdw.Collections;

namespace Fdw.Services.Etl.Predicates;

/// <summary>
/// Base class for the shapes a value in a record can have. A shape counts as true, is no point in time and reads as a number only
/// through its text until it says otherwise.
/// </summary>
public abstract class RecordValueShapeBase : TypeOptionBase<RecordValueShapeBase>, IRecordValueShape
{
    /// <summary>Initializes a new instance of the <see cref="RecordValueShapeBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    protected RecordValueShapeBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Matches(object value);

    /// <inheritdoc />
    public virtual bool IsTrue(object value) => true;

    /// <inheritdoc />
    public virtual bool TryMoment(object value, out DateTimeOffset moment)
    {
        moment = default;
        return false;
    }

    /// <inheritdoc />
    public virtual bool TryDecimal(object value, out decimal number) => decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out number);
}
