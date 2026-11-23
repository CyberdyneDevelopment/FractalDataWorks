using System;
using Fdw.Collections;

namespace Fdw.Services.Etl.Execution;

/// <summary>A shape a stored run time can have, and how it is read as a moment.</summary>
public interface IRunTimeShape : ITypeOption<int, RunTimeShapeBase>
{
    /// <summary>Whether the value has this shape.</summary>
    /// <param name="value">The stored run time.</param>
    /// <returns><c>true</c> when it does.</returns>
    bool Matches(object? value);

    /// <summary>Reads the value as a moment.</summary>
    /// <param name="value">A value this shape <see cref="Matches"/>.</param>
    /// <returns>The moment.</returns>
    DateTimeOffset ToOffset(object value);
}
