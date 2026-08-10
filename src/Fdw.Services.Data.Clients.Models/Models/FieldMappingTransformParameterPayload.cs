using System;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Represents a single parameter value for a field mapping transform step.
/// </summary>
public sealed class FieldMappingTransformParameterPayload
{
    /// <summary>Gets or sets the parameter identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the parent transform identifier.</summary>
    public Guid TransformId { get; set; }

    /// <summary>Gets or sets the parameter name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the parameter value.</summary>
    public string Value { get; set; } = string.Empty;
}
