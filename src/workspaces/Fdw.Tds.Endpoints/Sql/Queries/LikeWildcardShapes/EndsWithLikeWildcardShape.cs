using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>% at the start only.</summary>
[TypeOption(typeof(LikeWildcardShapes), "EndsWith")]
[ExcludeFromCodeCoverage]
public sealed class EndsWithLikeWildcardShape : LikeWildcardShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="EndsWithLikeWildcardShape"/> class.</summary>
    public EndsWithLikeWildcardShape()
        : base("EndsWith")
    {
    }

    /// <inheritdoc />
    public override bool Holds(bool leading, bool trailing) => leading && !trailing;
}
