using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Tags.Results;

/// <summary>Tag 'name' belongs to ownerKind 'ownerId' and cannot be moved to another owner (FDW-3669).</summary>
[TypeOption(typeof(TagsResultCodes), "TagOwnerChangeRefused", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TagOwnerChangeRefusedCode : TagsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="TagOwnerChangeRefusedCode"/> class.</summary>
    public TagOwnerChangeRefusedCode()
        : base(21304, "TagOwnerChangeRefused",
            ResultSeverities.ByName("Error"),
            "Tag '{name}' belongs to {ownerKind} '{ownerId}' and cannot be moved to another owner",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
