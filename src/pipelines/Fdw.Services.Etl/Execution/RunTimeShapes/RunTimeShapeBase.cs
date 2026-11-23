using System;
using Fdw.Collections;

namespace Fdw.Services.Etl.Execution;

/// <summary>Base class for the shapes a stored run time can have.</summary>
public abstract class RunTimeShapeBase : TypeOptionBase<RunTimeShapeBase>, IRunTimeShape
{
    /// <summary>Initializes a new instance of the <see cref="RunTimeShapeBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    protected RunTimeShapeBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Matches(object? value);

    /// <inheritdoc />
    public abstract DateTimeOffset ToOffset(object value);
}
