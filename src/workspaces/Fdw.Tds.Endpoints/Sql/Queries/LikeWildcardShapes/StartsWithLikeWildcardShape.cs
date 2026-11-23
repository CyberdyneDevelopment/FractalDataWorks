using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>% at the end only.</summary>
[TypeOption(typeof(LikeWildcardShapes), "StartsWith")]
[ExcludeFromCodeCoverage]
public sealed class StartsWithLikeWildcardShape : LikeWildcardShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="StartsWithLikeWildcardShape"/> class.</summary>
    public StartsWithLikeWildcardShape()
        : base("StartsWith")
    {
    }

    /// <inheritdoc />
    public override bool Holds(bool leading, bool trailing) => !leading && trailing;
}
