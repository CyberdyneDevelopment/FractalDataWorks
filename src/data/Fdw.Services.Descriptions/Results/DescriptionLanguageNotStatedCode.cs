using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Descriptions.Results;

/// <summary>Nobody stated the language of today's descriptions: the setting resolves to nothing (FDW-6120).</summary>
[TypeOption(typeof(DescriptionsResultCodes), "DescriptionLanguageNotStated", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DescriptionLanguageNotStatedCode : DescriptionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DescriptionLanguageNotStatedCode"/> class.</summary>
    public DescriptionLanguageNotStatedCode()
        : base(61400, "DescriptionLanguageNotStated",
            ResultSeverities.ByName("Error"),
            "The setting '{setting}' states no language, so the descriptions cannot be read or written without one being named",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
