using Fdw.Collections;

namespace Fdw.Data.Transformations;

/// <summary>Base for a rounding mode.</summary>
public abstract class RoundingTypeBase : TypeOptionBase<RoundingTypeBase>, IRoundingType
{
    /// <summary>Initializes a new instance of the <see cref="RoundingTypeBase"/> class.</summary>
    protected RoundingTypeBase(string name)
        : base(name, "Rounding")
    {
    }

    /// <inheritdoc/>
    public abstract decimal Round(decimal value, int precision);
}
