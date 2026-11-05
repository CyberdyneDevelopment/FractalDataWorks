using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Tags.Results;

/// <summary>kind 'thingId' already carries tag 'name' (FDW-3669).</summary>
[TypeOption(typeof(TagsResultCodes), "TagLinkExists", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TagLinkExistsCode : TagsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="TagLinkExistsCode"/> class.</summary>
    public TagLinkExistsCode()
        : base(41307, "TagLinkExists",
            ResultSeverities.ByName("Error"),
            "{kind} '{thingId}' already carries tag '{name}'",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
