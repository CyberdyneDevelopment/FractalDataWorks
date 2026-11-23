using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Descriptions.Results;

/// <summary>A description names no thing: the thing's Id is empty (FDW-6120).</summary>
[TypeOption(typeof(DescriptionsResultCodes), "DescriptionThingMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DescriptionThingMissingCode : DescriptionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DescriptionThingMissingCode"/> class.</summary>
    public DescriptionThingMissingCode()
        : base(21402, "DescriptionThingMissing",
            ResultSeverities.ByName("Error"),
            "A {type} description names no thing: the thing's Id is empty",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
