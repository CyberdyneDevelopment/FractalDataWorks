using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>No % at either end.</summary>
[TypeOption(typeof(LikeWildcardShapes), "Equal")]
[ExcludeFromCodeCoverage]
public sealed class EqualLikeWildcardShape : LikeWildcardShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="EqualLikeWildcardShape"/> class.</summary>
    public EqualLikeWildcardShape()
        : base("Equal")
    {
    }

    /// <inheritdoc />
    public override bool Holds(bool leading, bool trailing) => !leading && !trailing;
}
