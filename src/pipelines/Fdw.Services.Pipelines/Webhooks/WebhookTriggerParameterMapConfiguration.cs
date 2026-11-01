using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Configuration;
using Fdw.Data;

namespace Fdw.Services.Pipelines.Webhooks;

/// <summary>
/// One mapping of a webhook trigger (FDW-1884): the value at <see cref="BodyPath"/> in the JSON body of a call is given to the pipeline
/// parameter <see cref="ParameterName"/>. Cascade child of <see cref="WebhookTriggerConfiguration"/> (pipe.WebhookTriggerParameterMap).
/// </summary>
[ExcludeFromCodeCoverage]
[GenerateMapper]
[ManagedConfiguration(ServiceCategory = "WebhookTrigger")]
public sealed partial class WebhookTriggerParameterMapConfiguration : IGenericConfiguration
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>Gets or sets the mapping's place among the others.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the dotted path of property names in the body (<c>order.region</c>).</summary>
    public string BodyPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the pipeline parameter the value is given to.</summary>
    public string ParameterName { get; set; } = string.Empty;
}
