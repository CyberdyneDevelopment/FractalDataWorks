using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Protocols.FilterNodes;

/// <summary>A condition whose operator the protocol cannot express: the refusal names the operator and the field.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(RestFilterNodeHandlers), "Condition")]
public sealed class ConditionRestFilterNodeHandler : RestFilterNodeHandlerBase
{
    /// <summary>Initializes a new instance of the <see cref="ConditionRestFilterNodeHandler"/> class.</summary>
    public ConditionRestFilterNodeHandler()
        : base("Condition")
    {
    }

    /// <inheritdoc/>
    public override ResultDetails Describe(IFilterNode node, string protocolName) =>
        node is IFilterCondition condition
            ? ResultDetails.Create(
                "Protocol", protocolName,
                "Operator", condition.Operator.Name,
                "Field", condition.PropertyName)
            : throw new InvalidOperationException($"Filter node '{node.GetType().Name}' is not a condition.");
}
