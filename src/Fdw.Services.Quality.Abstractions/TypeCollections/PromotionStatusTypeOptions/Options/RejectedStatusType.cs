using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.PromotionStatusTypeOptions.Options;

/// <summary>
/// Rejected status type indicating a promotion was rejected by an approver.
/// </summary>
[TypeOption(typeof(PromotionStatusTypes), "Rejected")]
[ExcludeFromCodeCoverage]
public sealed class RejectedStatusType : PromotionStatusTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RejectedStatusType"/> class.
    /// </summary>
    public RejectedStatusType()
        : base(
            id: 3,
            name: "Rejected",
            isTerminal: true,
            isSuccess: false,
            allowsExecution: false)
    {
    }
}
