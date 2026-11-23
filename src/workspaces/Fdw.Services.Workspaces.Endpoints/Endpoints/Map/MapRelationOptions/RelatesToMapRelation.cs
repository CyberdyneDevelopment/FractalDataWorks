using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>Two nodes a workspace relates; no data moves.</summary>
[TypeOption(typeof(MapRelationOptions), "relates_to")]
[ExcludeFromCodeCoverage]
public sealed class RelatesToMapRelation : MapRelationOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="RelatesToMapRelation"/> class.</summary>
    public RelatesToMapRelation()
        : base("relates_to")
    {
    }
}
