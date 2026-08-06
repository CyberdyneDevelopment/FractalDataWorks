using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.PromotionStatusTypeOptions.Options;

/// <summary>
/// Pending status type indicating a promotion is awaiting approval.
/// </summary>
[TypeOption(typeof(PromotionStatusTypes), "Pending")]
[ExcludeFromCodeCoverage]
public sealed class PendingStatusType : PromotionStatusTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PendingStatusType"/> class.
    /// </summary>
    public PendingStatusType()
        : base(
            id: 1,
            name: "Pending",
            isTerminal: false,
            isSuccess: false,
            allowsExecution: false)
    {
    }
}
