using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Tags.Results;

/// <summary>A list of kind was asked to filter by tags 'tags', but this host does not run the Tags service (FDW-3816).</summary>
[TypeOption(typeof(TagsResultCodes), "TagFilterUnavailable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TagFilterUnavailableCode : TagsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="TagFilterUnavailableCode"/> class.</summary>
    public TagFilterUnavailableCode()
        : base(61309, "TagFilterUnavailable",
            ResultSeverities.ByName("Error"),
            "The {kind} list cannot be filtered by tags '{tags}': this host does not run the Tags service",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
