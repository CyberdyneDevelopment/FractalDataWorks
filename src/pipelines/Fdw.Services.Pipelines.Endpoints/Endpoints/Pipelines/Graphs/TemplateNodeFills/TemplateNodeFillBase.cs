using System;
using System.Text.Json.Nodes;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Base class for the kinds of JSON node whose children a template fills.</summary>
public abstract class TemplateNodeFillBase : TypeOptionBase<TemplateNodeFillBase>, ITemplateNodeFill
{
    /// <summary>Initializes a new instance of the <see cref="TemplateNodeFillBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    protected TemplateNodeFillBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Matches(JsonNode? node);

    /// <inheritdoc />
    public abstract IGenericResult Fill(JsonNode node, Func<JsonNode?, IGenericResult<JsonNode?>> fillChild);
}
