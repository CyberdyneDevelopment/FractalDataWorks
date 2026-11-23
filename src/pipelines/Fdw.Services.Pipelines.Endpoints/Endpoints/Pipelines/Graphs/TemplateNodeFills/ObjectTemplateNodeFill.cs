using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Nodes;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>A JSON object: each property is filled.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(TemplateNodeFills), "Object")]
public sealed class ObjectTemplateNodeFill : TemplateNodeFillBase
{
    /// <summary>Initializes a new instance of the <see cref="ObjectTemplateNodeFill"/> class.</summary>
    public ObjectTemplateNodeFill()
        : base("Object")
    {
    }

    /// <inheritdoc />
    public override bool Matches(JsonNode? node) => node is JsonObject;

    /// <inheritdoc />
    public override IGenericResult Fill(JsonNode node, Func<JsonNode?, IGenericResult<JsonNode?>> fillChild)
    {
        var properties = (JsonObject)node;
        foreach (var key in properties.Select(pair => pair.Key).ToList())
        {
            var replaced = fillChild(properties[key]);
            if (replaced.IsFailure)
            {
                return replaced;
            }

            properties[key] = replaced.Value;
        }

        return GenericResult.Success();
    }
}
