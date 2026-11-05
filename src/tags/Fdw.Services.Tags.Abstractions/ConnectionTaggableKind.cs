using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Tags.Abstractions;

/// <summary>A Connection that carries tags in <c>conn.ConnectionTag</c>. This kind is tenant-wide: it is in no workspace, so it takes tenant tags only.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TaggableKinds), "Connection")]
public sealed class ConnectionTaggableKind : TaggableKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ConnectionTaggableKind"/> class.</summary>
    public ConnectionTaggableKind()
        : base("Connection", "conn", "ConnectionTag", "ConnectionId", string.Empty)
    {
    }
}
