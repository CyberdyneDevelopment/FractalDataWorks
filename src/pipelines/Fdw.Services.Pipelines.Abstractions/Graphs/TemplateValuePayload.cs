namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>
/// The value given for one parameter of a template when a pipeline is made from it (FDW-3379).
/// </summary>
public class TemplateValuePayload
{
    /// <summary>Gets or sets the name of the template's parameter.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the value as text; it is read as the type the parameter declares.</summary>
    public string Value { get; set; } = string.Empty;
}
