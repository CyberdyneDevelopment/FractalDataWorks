using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.OData.FilterNodes;

/// <summary>Writes a single condition: the property, the operator's OData word and its formatted value.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(ODataFilterNodeHandlers), "Condition")]
public sealed class ConditionODataFilterNodeHandler : ODataFilterNodeHandlerBase
{
    /// <summary>Initializes a new instance of the <see cref="ConditionODataFilterNodeHandler"/> class.</summary>
    public ConditionODataFilterNodeHandler()
        : base("Condition")
    {
    }

    /// <inheritdoc/>
    public override string Build(IFilterNode node) =>
        node is FilterCondition condition
            ? ODataCommandTranslatorBase.BuildODataCondition(condition)
            : throw new InvalidOperationException($"Unknown filter node type: {node.GetType().Name}");
}
