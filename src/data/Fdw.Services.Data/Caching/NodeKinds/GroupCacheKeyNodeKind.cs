using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections.Attributes;
using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data.Caching;

/// <summary>A group.</summary>
[TypeOption(typeof(CacheKeyNodeKinds), "Group")]
[ExcludeFromCodeCoverage]
public sealed class GroupCacheKeyNodeKind : CacheKeyNodeKindBase
{
    /// <summary>Initializes a new instance of the <see cref="GroupCacheKeyNodeKind"/> class.</summary>
    public GroupCacheKeyNodeKind()
        : base("Group")
    {
    }

    /// <inheritdoc />
    public override bool Holds(IFilterNode node) => node is FilterGroup;

    /// <inheritdoc />
    public override void Write(StringBuilder text, IFilterNode node)
    {
        var group = (FilterGroup)node;
        text.Append("g(");
        CacheKeyText.Text(text, "l", group.Operator.Name);
        foreach (var child in group.Nodes)
            CacheKeyNodeKinds.Append(text, child);
        text.Append(')');
    }
}
