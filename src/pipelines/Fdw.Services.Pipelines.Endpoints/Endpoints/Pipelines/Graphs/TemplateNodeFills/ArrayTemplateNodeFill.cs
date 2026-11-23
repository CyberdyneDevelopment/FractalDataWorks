using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>A JSON array: each item is filled.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TemplateNodeFills), "Array")]
public sealed class ArrayTemplateNodeFill : TemplateNodeFillBase
{
    /// <summary>Initializes a new instance of the <see cref="ArrayTemplateNodeFill"/> class.</summary>
    public ArrayTemplateNodeFill()
        : base("Array")
    {
    }

    /// <inheritdoc />
    public override bool Matches(JsonNode? node) => node is JsonArray;

    /// <inheritdoc />
    public override IGenericResult Fill(JsonNode node, Func<JsonNode?, IGenericResult<JsonNode?>> fillChild)
    {
        var items = (JsonArray)node;
        for (var index = 0; index < items.Count; index++)
        {
            var replaced = fillChild(items[index]);
            if (replaced.IsFailure)
            {
                return replaced;
            }

            items[index] = replaced.Value;
        }

        return GenericResult.Success();
    }
}
