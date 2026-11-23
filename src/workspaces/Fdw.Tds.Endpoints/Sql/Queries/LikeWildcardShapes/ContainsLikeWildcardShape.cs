using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>% at both ends.</summary>
[TypeOption(typeof(LikeWildcardShapes), "Contains")]
[ExcludeFromCodeCoverage]
public sealed class ContainsLikeWildcardShape : LikeWildcardShapeBase
{
    /// <summary>Initializes a new instance of the <see cref="ContainsLikeWildcardShape"/> class.</summary>
    public ContainsLikeWildcardShape()
        : base("Contains")
    {
    }

    /// <inheritdoc />
    public override bool Holds(bool leading, bool trailing) => leading && trailing;
}
