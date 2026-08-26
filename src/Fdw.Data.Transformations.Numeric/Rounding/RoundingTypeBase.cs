using Fdw.Collections;

namespace Fdw.Data.Transformations;

/// <summary>Base for a rounding mode.</summary>
public abstract class RoundingTypeBase : TypeOptionBase<int, RoundingTypeBase>, IRoundingType
{
    /// <summary>Initializes a new instance of the <see cref="RoundingTypeBase"/> class.</summary>
    protected RoundingTypeBase(int id, string name)
        : base(id, name, "Rounding")
    {
    }

    /// <inheritdoc/>
    public abstract decimal Round(decimal value, int precision);
}
