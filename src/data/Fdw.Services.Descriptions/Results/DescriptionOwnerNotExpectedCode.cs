using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Descriptions.Results;

/// <summary>A description of a type that has no owner states an owner's name (FDW-6120).</summary>
[TypeOption(typeof(DescriptionsResultCodes), "DescriptionOwnerNotExpected", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DescriptionOwnerNotExpectedCode : DescriptionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DescriptionOwnerNotExpectedCode"/> class.</summary>
    public DescriptionOwnerNotExpectedCode()
        : base(21407, "DescriptionOwnerNotExpected",
            ResultSeverities.ByName("Error"),
            "A {type} description of {thingId} states the owner '{ownerName}', but a {type} has no owner",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
