using System;
using Fdw.Collections;

namespace Fdw.Operations.Endpoints.Health;

/// <summary>A unit a health query's time window is stated in.</summary>
public interface IHealthWindowUnit : ITypeOption<int, HealthWindowUnitBase>
{
    /// <summary>Gets the character that ends a window text, naming this unit (<c>5m</c>, <c>1h</c>).</summary>
    char Symbol { get; }

    /// <summary>Gets the length of a window of the given count of this unit.</summary>
    /// <param name="count">How many units long.</param>
    /// <returns>The window's length.</returns>
    TimeSpan Window(int count);
}
