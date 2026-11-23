using Fdw.Collections;

namespace Fdw.Services.Etl.Execution;

/// <summary>A shape a run parameter's value can have, and how it is written as text.</summary>
public interface IParameterTextShape : ITypeOption<int, ParameterTextShapeBase>
{
    /// <summary>Whether the value has this shape.</summary>
    /// <param name="value">The parameter's value.</param>
    /// <returns><c>true</c> when it does.</returns>
    bool Matches(object value);

    /// <summary>Writes the value as text, in the invariant culture.</summary>
    /// <param name="value">A value this shape <see cref="Matches"/>.</param>
    /// <returns>The text.</returns>
    string Write(object value);
}
