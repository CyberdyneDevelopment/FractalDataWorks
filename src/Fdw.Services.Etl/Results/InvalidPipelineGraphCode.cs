using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Etl.Results;

/// <summary>
/// A graph pipeline in a save request was refused: a component of an unknown kind, a setting its kind requires left out or one
/// it does not take, a link to a node that does not exist, or a shape the engine cannot run.
/// </summary>
/// <remarks>
/// 20003: the graph is the caller's input, so the answer is a 400 carrying the reason, never a 500. The reason is the message the
/// check logged when it refused, which names the component and the setting.
/// </remarks>
[TypeOption(typeof(EtlResultCodes), "InvalidPipelineGraph", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidPipelineGraphCode : EtlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPipelineGraphCode"/> class.
    /// </summary>
    public InvalidPipelineGraphCode()
        : base(20003, "InvalidPipelineGraph",
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
