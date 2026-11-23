using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Descriptions.Results;

/// <summary>'type' is not a description type (FDW-6120).</summary>
[TypeOption(typeof(DescriptionsResultCodes), "DescriptionTypeUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DescriptionTypeUnknownCode : DescriptionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DescriptionTypeUnknownCode"/> class.</summary>
    public DescriptionTypeUnknownCode()
        : base(21400, "DescriptionTypeUnknown",
            ResultSeverities.ByName("Error"),
            "'{type}' is not a description type",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
