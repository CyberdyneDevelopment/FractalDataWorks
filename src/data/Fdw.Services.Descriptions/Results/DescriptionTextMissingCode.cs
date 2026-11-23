using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Descriptions.Results;

/// <summary>A description has no text: a thing with no description has no row (FDW-6120).</summary>
[TypeOption(typeof(DescriptionsResultCodes), "DescriptionTextMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DescriptionTextMissingCode : DescriptionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DescriptionTextMissingCode"/> class.</summary>
    public DescriptionTextMissingCode()
        : base(21404, "DescriptionTextMissing",
            ResultSeverities.ByName("Error"),
            "The {type} description of {thingId} in {language} has no text; delete it instead",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
