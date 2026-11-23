using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Descriptions.Results;

/// <summary>A description names no thing: the thing's name is empty (FDW-6120).</summary>
[TypeOption(typeof(DescriptionsResultCodes), "DescriptionThingNameMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DescriptionThingNameMissingCode : DescriptionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DescriptionThingNameMissingCode"/> class.</summary>
    public DescriptionThingNameMissingCode()
        : base(21405, "DescriptionThingNameMissing",
            ResultSeverities.ByName("Error"),
            "The {type} description of {thingId} names no thing: the thing's name is empty",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
