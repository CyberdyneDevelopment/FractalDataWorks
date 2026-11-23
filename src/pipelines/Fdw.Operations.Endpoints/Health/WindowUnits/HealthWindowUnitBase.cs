using System;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Operations.Endpoints.Health;

/// <summary>Base class for the units a health query's time window is stated in.</summary>
public abstract class HealthWindowUnitBase : TypeOptionBase<HealthWindowUnitBase>, IHealthWindowUnit
{
    private readonly TimeSpan _unit;

    /// <summary>Initializes a new instance of the <see cref="HealthWindowUnitBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="symbol">The character that ends a window text, naming the unit.</param>
    /// <param name="unit">The length of one unit.</param>
    protected HealthWindowUnitBase(string name, char symbol, TimeSpan unit)
        : base(name)
    {
        Symbol = symbol;
        _unit = unit;
    }

    /// <inheritdoc />
    [TypeLookup("BySymbol")]
    public char Symbol { get; }

    /// <inheritdoc />
    public TimeSpan Window(int count) => _unit * count;
}
