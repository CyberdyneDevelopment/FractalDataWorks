using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Descriptions.Results;

/// <summary>The parts stored for a description are not 1, 2, 3 ... in order, so they cannot be joined into the text that was written (FDW-6123).</summary>
[TypeOption(typeof(DescriptionsResultCodes), "DescriptionPartsBroken", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DescriptionPartsBrokenCode : DescriptionsResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DescriptionPartsBrokenCode"/> class.</summary>
    public DescriptionPartsBrokenCode()
        : base(91400, "DescriptionPartsBroken",
            ResultSeverities.ByName("Error"),
            "The {type} description of {thingId} in {language} is stored as parts {ordinals}, which are not 1 to n without a gap; it is not joined, because a text with a part missing is not the text that was written",
            isRetryable: false)
    {
    }

    /// <inheritdoc />
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}
