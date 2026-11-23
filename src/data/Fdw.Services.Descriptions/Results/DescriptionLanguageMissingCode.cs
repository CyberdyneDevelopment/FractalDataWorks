using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Descriptions.Results;

/// <summary>A description names no language (FDW-6120).</summary>
[TypeOption(typeof(DescriptionsResultCodes), "DescriptionLanguageMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DescriptionLanguageMissingCode : DescriptionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DescriptionLanguageMissingCode"/> class.</summary>
    public DescriptionLanguageMissingCode()
        : base(21403, "DescriptionLanguageMissing",
            ResultSeverities.ByName("Error"),
            "A {type} description of {thingId} names no language",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
