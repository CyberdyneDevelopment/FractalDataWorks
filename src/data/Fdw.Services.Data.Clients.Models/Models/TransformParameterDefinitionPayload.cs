namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Describes a single parameter that a transform type expects.
/// Mirrors OperationParameterDefinition for the wire.
/// </summary>
public sealed class TransformParameterDefinitionPayload
{
    /// <summary>Gets or sets the parameter name (dictionary key).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the parameter kind (e.g., Scalar, Field).</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this parameter is required.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Gets or sets the display name for UI rendering.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets optional help text for UI tooltips.</summary>
    public string? HelpText { get; set; }
}
