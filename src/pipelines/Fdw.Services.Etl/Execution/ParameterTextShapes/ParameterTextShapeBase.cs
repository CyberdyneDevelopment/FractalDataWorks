using Fdw.Collections;

namespace Fdw.Services.Etl.Execution;

/// <summary>Base class for the shapes a run parameter's value can have and how each is written as text.</summary>
public abstract class ParameterTextShapeBase : TypeOptionBase<ParameterTextShapeBase>, IParameterTextShape
{
    /// <summary>Initializes a new instance of the <see cref="ParameterTextShapeBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    protected ParameterTextShapeBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Matches(object value);

    /// <inheritdoc />
    public abstract string Write(object value);
}
