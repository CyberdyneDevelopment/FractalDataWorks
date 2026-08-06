using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.PromotionStatusTypeOptions.Options;

/// <summary>
/// Completed status type indicating a promotion has successfully completed.
/// </summary>
[TypeOption(typeof(PromotionStatusTypes), "Completed")]
[ExcludeFromCodeCoverage]
public sealed class CompletedStatusType : PromotionStatusTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompletedStatusType"/> class.
    /// </summary>
    public CompletedStatusType()
        : base(
            id: 5,
            name: "Completed",
            isTerminal: true,
            isSuccess: true,
            allowsExecution: false)
    {
    }
}
