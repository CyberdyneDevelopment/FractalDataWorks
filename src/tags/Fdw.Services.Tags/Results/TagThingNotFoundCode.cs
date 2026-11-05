using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Tags.Results;

/// <summary>No kind 'thingId' was found, so it cannot be tagged (FDW-3669).</summary>
[TypeOption(typeof(TagsResultCodes), "TagThingNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TagThingNotFoundCode : TagsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="TagThingNotFoundCode"/> class.</summary>
    public TagThingNotFoundCode()
        : base(31306, "TagThingNotFound",
            ResultSeverities.ByName("Error"),
            "No {kind} '{thingId}' was found, so it cannot be tagged",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
