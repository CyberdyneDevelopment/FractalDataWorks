using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Tags.Results;

/// <summary>A tag named 'name' already exists in this tenant, owned by ownerKind 'ownerId' (FDW-3669).</summary>
[TypeOption(typeof(TagsResultCodes), "TagNameTaken", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TagNameTakenCode : TagsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="TagNameTakenCode"/> class.</summary>
    public TagNameTakenCode()
        : base(41303, "TagNameTaken",
            ResultSeverities.ByName("Error"),
            "A tag named '{name}' already exists in this tenant, owned by {ownerKind} '{ownerId}'",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
