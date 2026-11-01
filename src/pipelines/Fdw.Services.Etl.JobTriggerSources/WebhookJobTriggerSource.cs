using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Etl.JobTriggerSources;

/// <summary>The "Webhook" job trigger source — a run started by an outside system calling a webhook trigger's address (FDW-1884).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(JobTriggerSourceTypes), "Webhook")]
public sealed class WebhookJobTriggerSource : JobTriggerSourceBase
{
    /// <summary>Initializes a new instance of the <see cref="WebhookJobTriggerSource"/> class.</summary>
    public WebhookJobTriggerSource() : base("Webhook") { }
}
