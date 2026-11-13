using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Fdw.Collections;
using Fdw.Conventions;
using Fdw.Results;
using Fdw.Services.Data.Execution;
using Fdw.Services.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Data.Graphs;

/// <summary>
/// Registers the runner that reads a Graph data set (FDW-5291) in a host that references this package, and declares the default limits of a read.
/// </summary>
/// <remarks>
/// The limits are configuration. The defaults are declared here, on the option, and a value stated in the host's configuration under
/// <c>Pipelines:GraphDataSetRunner</c> (<c>MaxRows</c>, <c>MaxRunSeconds</c>, <c>LinkCapacity</c>, and for nested data sets <c>MaxDepth</c>, FDW-5299) wins; a stated value that is not a whole number refuses registration.
/// No code caps a read by a number written elsewhere.
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(PipelineServiceTypes), "GraphDataSetRunner")]
public sealed class GraphDataSetRunnerServiceType : PipelineServiceTypeBase
{
    /// <summary>The section a host states the limits in.</summary>
    public const string Section = "Pipelines:GraphDataSetRunner";

    /// <summary>The most rows a read of a Graph data set may produce when the host states no limit.</summary>
    [PlatformDefault]
    public const int DefaultMaxRows = 100_000;

    /// <summary>The most seconds a read of a Graph data set may run when the host states no limit.</summary>
    [PlatformDefault]
    public const int DefaultMaxRunSeconds = 60;

    /// <summary>The batches a link of a graph buffers before a writer waits, when the host states no number.</summary>
    [PlatformDefault]
    public const int DefaultLinkCapacity = 4;

    /// <summary>The rows a preview of a Graph data set draft reads from each source when the caller states no number (FDW-5454).</summary>
    [PlatformDefault]
    public const int DefaultPreviewRowsPerSource = 100;

    /// <summary>The most levels of data sets a Graph data set may nest below itself when the host states no limit (FDW-5299).</summary>
    [PlatformDefault]
    public const int DefaultMaxDepth = 8;

    /// <summary>Initializes a new instance of the <see cref="GraphDataSetRunnerServiceType"/> class.</summary>
    public GraphDataSetRunnerServiceType()
        : base(
            "GraphDataSetRunner",
            Section,
            "Graph data set runner",
            "Runs the graph of a Graph data set when it is read, under stated row and time limits")
    {
        Registration((builder, loggerFactory) =>
        {
            var rows = Stated(builder, nameof(GraphDataSetReadOptions.MaxRows), DefaultMaxRows);
            var seconds = Stated(builder, nameof(GraphDataSetReadOptions.MaxRunSeconds), DefaultMaxRunSeconds);
            var capacity = Stated(builder, nameof(GraphDataSetReadOptions.LinkCapacity), DefaultLinkCapacity);
            var depth = Stated(builder, nameof(GraphDataSetNestingOptions.MaxDepth), DefaultMaxDepth);
            var preview = Stated(builder, "PreviewRowsPerSource", DefaultPreviewRowsPerSource);
            if (rows is null || seconds is null || capacity is null || depth is null || preview is null)
            {
                return GenericResult<IHostApplicationBuilder>.Failure(Logging.GraphRunnerLog.LimitNotANumber((loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<GraphDataSetRunnerServiceType>(), Section));
            }

            builder.Services.TryAddSingleton(new GraphDataSetReadOptions(rows.Value, seconds.Value, capacity.Value));
            builder.Services.TryAddSingleton(new GraphDataSetNestingOptions(depth.Value));
            builder.Services.TryAddSingleton<IGraphDataSetSources, GraphDataSetSources>();
            builder.Services.TryAddSingleton<GraphDataSetRunner>();
            builder.Services.TryAddSingleton(new GraphDataSetPreviewOptions(preview.Value));
            builder.Services.TryAddSingleton<IGraphDataSetPreview, GraphDataSetPreviewer>();

            // Why the decorator is what the gateway gets: a nested Graph data set is read through the gateway, so every level comes back through the nesting guard.
            builder.Services.TryAddSingleton<IGraphDataSetRunner>(services => new GraphDataSetNestingRunner(
                services.GetRequiredService<GraphDataSetRunner>(),
                services.GetRequiredService<IGraphDataSetSources>(),
                services.GetRequiredService<GraphDataSetNestingOptions>(),
                services.GetService<ILogger<GraphDataSetNestingRunner>>()));
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });
    }

    // The stated value, the declared default when none is stated, and null when what is stated is not a whole number.
    private static int? Stated(IHostApplicationBuilder builder, string name, int declared) =>
        builder.Configuration[$"{Section}:{name}"] is { Length: > 0 } text
            ? int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null
            : declared;
}
