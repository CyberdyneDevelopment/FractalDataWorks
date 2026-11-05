using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Tags.Results;

/// <summary>'kind' is not a kind of thing that can be tagged (FDW-3669).</summary>
[TypeOption(typeof(TagsResultCodes), "TaggableKindUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TaggableKindUnknownCode : TagsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="TaggableKindUnknownCode"/> class.</summary>
    public TaggableKindUnknownCode()
        : base(21300, "TaggableKindUnknown",
            ResultSeverities.ByName("Error"),
            "'{kind}' is not a kind of thing that can be tagged",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
