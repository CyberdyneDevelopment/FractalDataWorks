using Fdw.Collections;

namespace Fdw.Data.Transformations;

/// <summary>A way of resolving the midpoint when rounding.</summary>
public interface IRoundingType : ITypeOption<int, RoundingTypeBase>
{
    /// <summary>Rounds <paramref name="value"/> to <paramref name="precision"/> decimal places.</summary>
    decimal Round(decimal value, int precision);
}
