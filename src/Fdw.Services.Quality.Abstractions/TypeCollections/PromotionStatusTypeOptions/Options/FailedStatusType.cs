using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.PromotionStatusTypeOptions.Options;

/// <summary>
/// Failed status type indicating a promotion execution has failed.
/// </summary>
[TypeOption(typeof(PromotionStatusTypes), "Failed")]
[ExcludeFromCodeCoverage]
public sealed class FailedStatusType : PromotionStatusTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailedStatusType"/> class.
    /// </summary>
    public FailedStatusType()
        : base(
            id: 6,
            name: "Failed",
            isTerminal: true,
            isSuccess: false,
            allowsExecution: false)
    {
    }
}
