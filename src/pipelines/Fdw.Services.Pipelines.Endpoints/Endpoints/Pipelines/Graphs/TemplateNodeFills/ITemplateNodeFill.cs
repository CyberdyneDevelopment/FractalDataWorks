using System;
using System.Text.Json.Nodes;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>A kind of JSON node whose children a template fills.</summary>
public interface ITemplateNodeFill : ITypeOption<int, TemplateNodeFillBase>
{
    /// <summary>Whether the node is of this kind.</summary>
    /// <param name="node">The node.</param>
    /// <returns><c>true</c> when this kind fills its children.</returns>
    bool Matches(JsonNode? node);

    /// <summary>Fills the children of a node of this kind in place, stopping at the first refusal.</summary>
    /// <param name="node">A node this kind <see cref="Matches"/>.</param>
    /// <param name="fillChild">Fills one child: a text is replaced by its filled text, an object or array is filled in place and kept.</param>
    /// <returns>Success, or the first refusal.</returns>
    IGenericResult Fill(JsonNode node, Func<JsonNode?, IGenericResult<JsonNode?>> fillChild);
}
