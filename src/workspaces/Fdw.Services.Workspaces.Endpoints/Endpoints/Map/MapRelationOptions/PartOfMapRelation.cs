using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A node is part of another; no data moves.</summary>
[TypeOption(typeof(MapRelationOptions), "part_of")]
[ExcludeFromCodeCoverage]
public sealed class PartOfMapRelation : MapRelationOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="PartOfMapRelation"/> class.</summary>
    public PartOfMapRelation()
        : base("part_of")
    {
    }
}
