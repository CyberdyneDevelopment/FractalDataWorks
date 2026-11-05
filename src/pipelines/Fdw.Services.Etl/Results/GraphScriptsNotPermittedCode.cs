using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Etl.Results;

/// <summary>
/// A graph pipeline in a save request holds script text and the caller may not write scripts (FDW-3676).
/// </summary>
/// <remarks>
/// A script in a pipeline is a script write by another route, so it needs the permission the stored scripts need. Category 10 (Forbidden), like
/// <c>WorkspaceWriteNotPermitted</c>. HTTP 403, carrying the reason, which names the pipeline and the components that hold the script.
/// </remarks>
[TypeOption(typeof(EtlResultCodes), "GraphScriptsNotPermitted", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphScriptsNotPermittedCode : EtlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphScriptsNotPermittedCode"/> class.
    /// </summary>
    public GraphScriptsNotPermittedCode()
        : base(100001, "GraphScriptsNotPermitted",
            ResultSeverities.ByName("Error"),
            "The graph is refused: {Reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
