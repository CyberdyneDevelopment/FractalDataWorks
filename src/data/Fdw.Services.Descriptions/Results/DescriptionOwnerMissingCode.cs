using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Descriptions.Results;

/// <summary>A description of a type that has an owner states no owner's name (FDW-6120).</summary>
[TypeOption(typeof(DescriptionsResultCodes), "DescriptionOwnerMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DescriptionOwnerMissingCode : DescriptionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DescriptionOwnerMissingCode"/> class.</summary>
    public DescriptionOwnerMissingCode()
        : base(21406, "DescriptionOwnerMissing",
            ResultSeverities.ByName("Error"),
            "A {type} description of {thingId} states no owner: a {type} belongs to an owner that names it",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
