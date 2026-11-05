using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Tags.Results;

/// <summary>This host does not carry the tenant, organization and workspace services, so it cannot list the owners a tag can be given to (FDW-4100).</summary>
[TypeOption(typeof(TagsResultCodes), "TagOwnersUnavailable", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TagOwnersUnavailableCode : TagsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="TagOwnersUnavailableCode"/> class.</summary>
    public TagOwnersUnavailableCode()
        : base(61312, "TagOwnersUnavailable",
            ResultSeverities.ByName("Error"),
            "The tag owners cannot be listed: this host does not run the tenant, organization and workspace services",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
