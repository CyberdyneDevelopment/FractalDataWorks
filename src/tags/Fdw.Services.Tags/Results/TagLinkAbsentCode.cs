using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Tags.Results;

/// <summary>kind 'thingId' does not carry tag 'name' (FDW-3669).</summary>
[TypeOption(typeof(TagsResultCodes), "TagLinkAbsent", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TagLinkAbsentCode : TagsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="TagLinkAbsentCode"/> class.</summary>
    public TagLinkAbsentCode()
        : base(31308, "TagLinkAbsent",
            ResultSeverities.ByName("Error"),
            "{kind} '{thingId}' does not carry tag '{name}'",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
