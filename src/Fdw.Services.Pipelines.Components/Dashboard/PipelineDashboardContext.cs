using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Fdw.UI.Providers;

namespace Fdw.Services.Pipelines.Components.Dashboard;

/// <summary>
/// Immutable context for the pipeline dashboard widget.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PipelineDashboardContext : ProviderContextBase
{
    /// <summary>Gets the total number of pipelines.</summary>
    public int TotalPipelines { get; init; }



}
