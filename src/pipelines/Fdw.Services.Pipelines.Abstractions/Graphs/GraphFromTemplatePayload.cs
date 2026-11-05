using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// The request that makes a pipeline from a template (FDW-3379).
/// </summary>
public class GraphFromTemplatePayload
{
    /// <summary>Gets or sets the name of the template.</summary>
    public string Template { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the new pipeline.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the values for the template's parameters. A parameter with no value stays a run parameter of the new pipeline.</summary>
    public IList<TemplateValuePayload> Values { get; set; } = [];
}
