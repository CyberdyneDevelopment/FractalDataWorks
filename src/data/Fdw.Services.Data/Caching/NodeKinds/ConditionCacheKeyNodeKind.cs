using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections.Attributes;
using Fdw.Data;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Data.Caching;

/// <summary>A condition.</summary>
[TypeOption(typeof(CacheKeyNodeKinds), "Condition")]
[ExcludeFromCodeCoverage]
public sealed class ConditionCacheKeyNodeKind : CacheKeyNodeKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ConditionCacheKeyNodeKind"/> class.</summary>
    public ConditionCacheKeyNodeKind()
        : base("Condition")
    {
    }

    /// <inheritdoc />
    public override bool Holds(IFilterNode node) => node is IFilterCondition;

    /// <inheritdoc />
    public override void Write(StringBuilder text, IFilterNode node)
    {
        var condition = (IFilterCondition)node;
        text.Append("c(");
        CacheKeyText.Text(text, "p", condition.PropertyName);
        CacheKeyText.Text(text, "o", condition.Operator.Name);
        CacheKeyValueKinds.Append(text, condition.Value);
        text.Append(')');
    }
}
