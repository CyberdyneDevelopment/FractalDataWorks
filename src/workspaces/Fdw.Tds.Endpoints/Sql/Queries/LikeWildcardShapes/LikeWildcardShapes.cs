using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>The ways a LIKE pattern can place its % marks at its ends: both, only the first, only the last, or neither.</summary>
/// <remarks>The four options cover every pair of flags, so <see cref="Of"/> finds one for any pattern; a test walks the four pairs.</remarks>
[TypeCollection(typeof(LikeWildcardShapeBase), typeof(ILikeWildcardShape), typeof(LikeWildcardShapes))]
[ExcludeFromCodeCoverage]
public abstract partial class LikeWildcardShapes : TypeCollectionBase<LikeWildcardShapeBase, ILikeWildcardShape>
{
    /// <summary>Finds the shape of a pattern.</summary>
    /// <param name="leading">Whether the pattern starts with %.</param>
    /// <param name="trailing">Whether the pattern ends with %.</param>
    /// <returns>The shape that holds the pattern; <c>NotFound</c> when no option does.</returns>
    public static ILikeWildcardShape Of(bool leading, bool trailing)
    {
        foreach (var shape in All())
        {
            if (shape.Holds(leading, trailing))
            {
                return shape;
            }
        }

        return NotFound;
    }
}
