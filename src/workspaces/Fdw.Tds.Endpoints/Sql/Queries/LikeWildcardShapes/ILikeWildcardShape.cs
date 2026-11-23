using Fdw.Collections;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>One way a LIKE pattern can place its % marks, and the filter operator the store is sent for a pattern that places them so.</summary>
public interface ILikeWildcardShape : ITypeOption<int, LikeWildcardShapeBase>
{
    /// <summary>Gets the name of the filter operator a pattern of this shape is.</summary>
    string FilterOperator { get; }

    /// <summary>Tells whether a pattern places its % marks as this shape does.</summary>
    /// <param name="leading">Whether the pattern starts with %.</param>
    /// <param name="trailing">Whether the pattern ends with %.</param>
    /// <returns>True when the pattern is of this shape.</returns>
    bool Holds(bool leading, bool trailing);
}
