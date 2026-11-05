using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Tags.Results;

/// <summary>The caller acts in no tenant, so there is no tenant to list as a tag owner (FDW-4100).</summary>
[TypeOption(typeof(TagsResultCodes), "TagOwnerTenantUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TagOwnerTenantUnknownCode : TagsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="TagOwnerTenantUnknownCode"/> class.</summary>
    public TagOwnerTenantUnknownCode()
        : base(31311, "TagOwnerTenantUnknown",
            ResultSeverities.ByName("Error"),
            "The caller acts in no tenant, so there is no tenant that could own a tag",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
