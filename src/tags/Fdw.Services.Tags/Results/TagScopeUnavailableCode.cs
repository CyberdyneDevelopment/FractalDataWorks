using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Tags.Results;

/// <summary>Tag 'name' cannot be put on kind 'thingId': this host registers no workspace provider to check where the tag applies (FDW-3816).</summary>
[TypeOption(typeof(TagsResultCodes), "TagScopeUnavailable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TagScopeUnavailableCode : TagsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="TagScopeUnavailableCode"/> class.</summary>
    public TagScopeUnavailableCode()
        : base(61310, "TagScopeUnavailable",
            ResultSeverities.ByName("Error"),
            "Tag '{name}' cannot be put on {kind} '{thingId}': this host registers no workspace provider, so it cannot check where the tag applies",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
