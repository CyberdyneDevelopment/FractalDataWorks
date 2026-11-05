using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Tags.Results;

/// <summary>Tag 'name' is owned by ownerKind 'ownerId' and cannot be put on kind 'thingId' (FDW-3669).</summary>
[TypeOption(typeof(TagsResultCodes), "TagNotInScope", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TagNotInScopeCode : TagsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="TagNotInScopeCode"/> class.</summary>
    public TagNotInScopeCode()
        : base(21305, "TagNotInScope",
            ResultSeverities.ByName("Error"),
            "Tag '{name}' is owned by {ownerKind} '{ownerId}' and cannot be put on {kind} '{thingId}': {reason}",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
