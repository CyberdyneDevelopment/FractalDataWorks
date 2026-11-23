using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Descriptions.Results;

/// <summary>'subType' is not a description sub-type (FDW-6120).</summary>
[TypeOption(typeof(DescriptionsResultCodes), "DescriptionSubTypeUnknown", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DescriptionSubTypeUnknownCode : DescriptionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DescriptionSubTypeUnknownCode"/> class.</summary>
    public DescriptionSubTypeUnknownCode()
        : base(21401, "DescriptionSubTypeUnknown",
            ResultSeverities.ByName("Error"),
            "'{subType}' is not a description sub-type",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
