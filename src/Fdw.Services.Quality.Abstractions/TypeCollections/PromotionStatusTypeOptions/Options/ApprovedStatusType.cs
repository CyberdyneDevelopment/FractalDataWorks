using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.PromotionStatusTypeOptions.Options;

/// <summary>
/// Approved status type indicating a promotion is approved and ready to execute.
/// </summary>
[TypeOption(typeof(PromotionStatusTypes), "Approved")]
[ExcludeFromCodeCoverage]
public sealed class ApprovedStatusType : PromotionStatusTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApprovedStatusType"/> class.
    /// </summary>
    public ApprovedStatusType()
        : base(
            id: 2,
            name: "Approved",
            isTerminal: false,
            isSuccess: false,
            allowsExecution: true)
    {
    }
}
