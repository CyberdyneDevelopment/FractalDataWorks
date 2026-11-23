using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>The base of every way a LIKE pattern can place its % marks.</summary>
[ExcludeFromCodeCoverage]
public abstract class LikeWildcardShapeBase : TypeOptionBase<LikeWildcardShapeBase>, ILikeWildcardShape
{
    /// <summary>Initializes a new instance of the <see cref="LikeWildcardShapeBase"/> class.</summary>
    /// <param name="name">The shape's name, which is the name of its filter operator.</param>
    protected LikeWildcardShapeBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public string FilterOperator => Name;

    /// <inheritdoc />
    public abstract bool Holds(bool leading, bool trailing);
}
