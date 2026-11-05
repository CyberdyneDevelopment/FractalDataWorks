using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Workspaces.Results;

/// <summary>
/// A lineage request could not be answered because a setting it needs is not set to a usable value for the caller (FDW-3850), such as the number
/// of pipelines a line of the groups names.
/// </summary>
/// <remarks>
/// Refused rather than answered with a number made up in code: a limit is configuration, and an absent one must not read as no limit or as some
/// default. The detail names the setting. It is the host's configuration that is at fault, not the request.
/// </remarks>
[TypeOption(typeof(WorkspacesResultCodes), "LineageSettingMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class LineageSettingMissingCode : WorkspacesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="LineageSettingMissingCode"/> class.</summary>
    public LineageSettingMissingCode()
        : base(20046, "LineageSettingMissing",
            ResultSeverities.ByName("Error"),
            "Lineage setting is not set to a usable value: {setting}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller which setting is at fault: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
